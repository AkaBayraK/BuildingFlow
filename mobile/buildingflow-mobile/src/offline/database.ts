import {readableOffline} from '../person-authorization/model.ts';
import { contextKey, parseSession, type Session } from '../auth/contracts.ts';
export type SqlValue = string | number | null;
export interface Database {
  execAsync(sql: string): Promise<void>;
  runAsync(sql: string, ...values: SqlValue[]): Promise<unknown>;
  getFirstAsync<T>(sql: string, ...values: SqlValue[]): Promise<T | null>;
}
export const SCHEMA_VERSION = 9;
export const MAX_CACHE_AGE_MS = 24 * 60 * 60 * 1000;
export type Snapshot = { session: Session; verifiedAt: number; selectedKey: string | null };
export async function migrate(db: Database) {
  const version = (await db.getFirstAsync<{ user_version: number }>('PRAGMA user_version'))?.user_version ?? 0;
  if (version > SCHEMA_VERSION) throw new Error('Yerel veri sürümü desteklenmiyor.');
  await db.execAsync('BEGIN IMMEDIATE');
  try {
    if (version < 1) await db.execAsync(`
      CREATE TABLE account (user_id TEXT PRIMARY KEY NOT NULL);
      CREATE TABLE cache_records (
        id TEXT PRIMARY KEY NOT NULL,
        user_id TEXT NOT NULL REFERENCES account(user_id),
        context_key TEXT NOT NULL,
        kind TEXT NOT NULL CHECK(kind = 'session'),
        payload TEXT NOT NULL,
        created_at INTEGER NOT NULL,
        updated_at INTEGER NOT NULL,
        verified_at INTEGER NOT NULL,
        version INTEGER NOT NULL DEFAULT 1,
        deleted_at INTEGER,
        sync_status TEXT NOT NULL CHECK(sync_status = 'cached'),
        UNIQUE(user_id, context_key, kind)
      );
      CREATE TABLE preferences (user_id TEXT PRIMARY KEY NOT NULL REFERENCES account(user_id), selected_key TEXT);
    `);
    if (version < 2) await db.execAsync('CREATE INDEX cache_scope ON cache_records(user_id, context_key, kind, deleted_at);');
    if (version < 3) await db.execAsync(`
      CREATE TABLE sync_outbox (operation_id TEXT PRIMARY KEY, user_id TEXT NOT NULL REFERENCES account(user_id), context_key TEXT NOT NULL, payload TEXT NOT NULL, status TEXT NOT NULL CHECK(status IN ('pending','sending','retry','sent','conflict','blocked','failed','cancelled')), attempts INTEGER NOT NULL DEFAULT 0, next_attempt_at INTEGER NOT NULL, created_at INTEGER NOT NULL, updated_at INTEGER NOT NULL, error_code INTEGER);
      CREATE INDEX sync_outbox_due ON sync_outbox(user_id,context_key,status,next_attempt_at);
      CREATE TABLE sync_requests (user_id TEXT NOT NULL REFERENCES account(user_id), context_key TEXT NOT NULL, record_id TEXT NOT NULL, payload TEXT NOT NULL, version INTEGER NOT NULL, deleted INTEGER NOT NULL CHECK(deleted IN (0,1)), modified_at TEXT NOT NULL, PRIMARY KEY(user_id,context_key,record_id));
      CREATE TABLE sync_state (user_id TEXT NOT NULL REFERENCES account(user_id), context_key TEXT NOT NULL, last_pulled_at INTEGER NOT NULL, PRIMARY KEY(user_id,context_key));
    `);
    if(version<4)await db.execAsync(`
      CREATE TABLE profile_cache (user_id TEXT PRIMARY KEY NOT NULL REFERENCES account(user_id),payload TEXT NOT NULL,checked_at INTEGER NOT NULL,photo_file TEXT);
      CREATE TABLE profile_jobs (operation_id TEXT PRIMARY KEY NOT NULL,user_id TEXT NOT NULL REFERENCES account(user_id),body_file TEXT NOT NULL,status TEXT NOT NULL CHECK(status IN ('pending','sending','retry','conflict','failed')),attempts INTEGER NOT NULL DEFAULT 0,next_attempt_at INTEGER NOT NULL,error_code INTEGER);
      CREATE UNIQUE INDEX profile_jobs_account ON profile_jobs(user_id);
      CREATE INDEX profile_jobs_scope ON profile_jobs(user_id,status,next_attempt_at);
      CREATE TABLE profile_settings (user_id TEXT PRIMARY KEY NOT NULL REFERENCES account(user_id),show_photo INTEGER NOT NULL DEFAULT 1 CHECK(show_photo IN (0,1)));
    `);
    if(version<5)await db.execAsync('CREATE TABLE owner_snapshots(user_id TEXT NOT NULL REFERENCES account(user_id),context_key TEXT NOT NULL,payload TEXT NOT NULL,checked_at INTEGER NOT NULL,PRIMARY KEY(user_id,context_key));');
    if(version<6)await db.execAsync(`
      CREATE TABLE request_drafts(user_id TEXT NOT NULL REFERENCES account(user_id),context_key TEXT NOT NULL,body_file TEXT NOT NULL,updated_at INTEGER NOT NULL,PRIMARY KEY(user_id,context_key));
      CREATE TABLE request_files(operation_id TEXT PRIMARY KEY NOT NULL REFERENCES sync_outbox(operation_id),user_id TEXT NOT NULL REFERENCES account(user_id),context_key TEXT NOT NULL,body_file TEXT NOT NULL);
      CREATE TABLE request_photo_cache(user_id TEXT NOT NULL REFERENCES account(user_id),context_key TEXT NOT NULL,record_id TEXT NOT NULL,photo_id TEXT NOT NULL,sha256 TEXT NOT NULL,body_file TEXT NOT NULL,checked_at INTEGER NOT NULL,PRIMARY KEY(user_id,context_key,record_id));
      CREATE INDEX request_files_scope ON request_files(user_id,context_key);
    `);
    if(version<7)await db.execAsync(`CREATE TABLE notice_jobs(user_id TEXT NOT NULL REFERENCES account(user_id),context_key TEXT NOT NULL,kind TEXT NOT NULL CHECK(kind IN ('ANNOUNCEMENT','MESSAGE')),item_id TEXT NOT NULL,status TEXT NOT NULL CHECK(status IN ('pending','retry','failed')),attempts INTEGER NOT NULL,next_at INTEGER NOT NULL,error INTEGER,PRIMARY KEY(user_id,context_key,kind,item_id)); CREATE INDEX notice_jobs_due ON notice_jobs(user_id,context_key,status,next_at);`);
    // İŞ18: consent texts/states, queued consent decisions and launch-gate reads; existing rows are untouched.
    if(version<8)await db.execAsync(`
      CREATE TABLE consent_cache(user_id TEXT PRIMARY KEY NOT NULL REFERENCES account(user_id),payload TEXT NOT NULL,checked_at INTEGER NOT NULL);
      CREATE TABLE consent_jobs(operation_id TEXT PRIMARY KEY NOT NULL,user_id TEXT NOT NULL REFERENCES account(user_id),type_code TEXT NOT NULL,action TEXT NOT NULL CHECK(action IN ('APPROVE','WITHDRAW')),payload TEXT NOT NULL,status TEXT NOT NULL CHECK(status IN ('pending','sending','retry','conflict','failed')),attempts INTEGER NOT NULL DEFAULT 0,next_attempt_at INTEGER NOT NULL,created_at INTEGER NOT NULL,error TEXT);
      CREATE INDEX consent_jobs_due ON consent_jobs(user_id,status,next_attempt_at,created_at);
      CREATE TABLE consent_seen(user_id TEXT NOT NULL REFERENCES account(user_id),text_id TEXT NOT NULL,seen_at INTEGER NOT NULL,PRIMARY KEY(user_id,text_id));
    `);
    if(version<9)await db.execAsync('CREATE TABLE IF NOT EXISTS portfolio_cache(user_id TEXT PRIMARY KEY NOT NULL REFERENCES account(user_id),payload TEXT NOT NULL,checked_at INTEGER NOT NULL);');
    await db.execAsync(`PRAGMA user_version = ${SCHEMA_VERSION}; COMMIT;`);
  } catch (error) { await db.execAsync('ROLLBACK'); throw error; }
}
export async function writeSnapshot(db: Database, value: Session, verifiedAt: number, id: string, selectedKey: string | null) {
  const session = parseSession(value);
  if (!Number.isSafeInteger(verifiedAt) || verifiedAt <= 0) throw new Error('Geçersiz güncellik zamanı.');
  if (selectedKey !== null && !session.contexts.some(c => contextKey(c) === selectedKey)) throw new Error('Yabancı bağlam.');
  const owner = await db.getFirstAsync<{ user_id: string }>('SELECT user_id FROM account LIMIT 1');
  if (owner && owner.user_id !== session.userId) throw new Error('Başka hesabın veritabanı.');
  // Whitelist fields: never serialize extra API fields, tokens, or credentials.
  const payload = JSON.stringify({ userId: session.userId, username: session.username, hasMore: false, contexts: session.contexts.map(c => ({ propertyId: c.propertyId, propertyName: c.propertyName, unitId: c.unitId, unitNumber: c.unitNumber, role: c.role,...(c.canonical?{canonical:c.canonical}:{}) })),...(session.authorization?{authorization:session.authorization}:{}) });
  await db.execAsync('BEGIN IMMEDIATE');
  try {
    await db.runAsync('INSERT OR IGNORE INTO account(user_id) VALUES (?)', session.userId);
    await db.runAsync(`INSERT INTO cache_records(id,user_id,context_key,kind,payload,created_at,updated_at,verified_at,version,deleted_at,sync_status)
      VALUES (?,?,'account','session',?,?,?,?,1,NULL,'cached')
      ON CONFLICT(user_id,context_key,kind) DO UPDATE SET payload=excluded.payload, updated_at=excluded.updated_at, verified_at=excluded.verified_at, version=cache_records.version+1, deleted_at=NULL`, id, session.userId, payload, verifiedAt, verifiedAt, verifiedAt);
    await db.runAsync('INSERT INTO preferences(user_id,selected_key) VALUES (?,?) ON CONFLICT(user_id) DO UPDATE SET selected_key=excluded.selected_key', session.userId, selectedKey);
    await db.execAsync('COMMIT');
  } catch (error) { await db.execAsync('ROLLBACK'); throw error; }
}
export async function readSnapshot(db: Database, userId: string, now: number): Promise<Snapshot | null> {
  const row = await db.getFirstAsync<{ payload: string; verified_at: number; selected_key: string | null }>(`SELECT c.payload,c.verified_at,p.selected_key FROM cache_records c LEFT JOIN preferences p ON p.user_id=c.user_id WHERE c.user_id=? AND c.context_key='account' AND c.kind='session' AND c.deleted_at IS NULL`, userId);
  if (!row || !Number.isSafeInteger(row.verified_at) || now < row.verified_at || now - row.verified_at > MAX_CACHE_AGE_MS) return null;
  const session = parseSession(JSON.parse(row.payload));
  if(session.authorization&&!readableOffline(session.authorization,now,row.verified_at))return null;
  if (session.userId !== userId) throw new Error('Hesap eşleşmiyor.');
  const selectedKey = session.contexts.some(c => contextKey(c) === row.selected_key) ? row.selected_key : null;
  return { session, verifiedAt: row.verified_at, selectedKey };
}
export async function writeSelection(db: Database, snapshot: Snapshot, key: string) {
  if (!snapshot.session.contexts.some(c => contextKey(c) === key)) throw new Error('Yabancı bağlam.');
  await db.runAsync('UPDATE preferences SET selected_key=? WHERE user_id=?', key, snapshot.session.userId);
}

// Account-scoped portfolio codes; a read keeps only contexts still present in the current authorization.
export async function writePortfolio(db: Database, userId: string, value: {fingerprint:string;access:Record<string,{status:string;capabilities:string[]}>}, checkedAt: number) {
  const owner = await db.getFirstAsync<{ user_id: string }>('SELECT user_id FROM account WHERE user_id=?', userId);
  if (!owner) throw new Error('Hesap önbelleği yok.');
  await db.runAsync('INSERT INTO portfolio_cache(user_id,payload,checked_at) VALUES (?,?,?) ON CONFLICT(user_id) DO UPDATE SET payload=excluded.payload,checked_at=excluded.checked_at', userId, JSON.stringify(value), checkedAt);
}
export async function readPortfolio(db: Database, userId: string, allowedKeys: readonly string[], now: number) {
  const row = await db.getFirstAsync<{ payload: string; checked_at: number }>('SELECT payload,checked_at FROM portfolio_cache WHERE user_id=?', userId);
  if (!row || now < row.checked_at || now - row.checked_at > MAX_CACHE_AGE_MS) return null;
  const value = JSON.parse(row.payload) as {fingerprint:string;access:Record<string,{status:string;capabilities:string[]}>};
  return { fingerprint: value.fingerprint, access: Object.fromEntries(Object.entries(value.access).filter(([key]) => allowedKeys.includes(key))), checkedAt: row.checked_at };
}
