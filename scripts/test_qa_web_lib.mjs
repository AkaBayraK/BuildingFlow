import test from 'node:test';
import assert from 'node:assert/strict';
import { judge, plan, reportLine, slug } from './qa-web-lib.mjs';

const clean = { overflow: 0, imagesWithoutAlt: 0, unnamedControls: 0, unlabelledFields: 0, tabsWithoutState: 0, headings: 1, alerts: [] };
const base = { documentStatus: 200, finalPath: '/dues', expectedPath: '/dues', exceptions: [], serverErrors: [], audit: clean, viewport: 'desktop' };

test('plan resolves property routes and skips unknown parameters', () => {
  const pages = [{ route: '/login' }, { route: '/dues' }, { route: '/properties/[id]' }, { route: '/properties/[id]/dues' }, { route: '/people/[tenantId]' }, { route: '/properties/[id]/buildings/[buildingId]' }];
  assert.deepEqual(plan(pages, 'p1').map(x => x.path), ['/dues', '/properties/p1', '/properties/p1/dues', null, null]);
  assert.match(plan(pages, null)[1].skip, /Yönetim alanı/);
  assert.equal(plan([{ route: '/sites' }], null)[0].expectedPath, '/properties');
  assert.match(plan([{ route: '/workspaces' }], null)[0].skip, /SESSION_CONTEXTS/);
  assert.equal(plan([{ route: '/workspaces' }], null, { sessionContexts: true })[0].path, '/workspaces');
  assert.deepEqual(plan(pages, 'p1', {}, ['/dues']).map(x => x.path), ['/dues']);
});

test('judge fails real defects and passes a clean page', () => {
  assert.deepEqual(judge(base), { status: 'PASS', problems: [] });
  assert.equal(judge({ ...base, finalPath: '/login' }).status, 'FAIL');
  assert.equal(judge({ ...base, documentStatus: 500 }).status, 'FAIL');
  assert.equal(judge({ ...base, exceptions: ['TypeError'] }).status, 'FAIL');
  assert.equal(judge({ ...base, serverErrors: ['GET /api 500'] }).status, 'FAIL');
  assert.equal(judge({ ...base, audit: { ...clean, overflow: 40 } }).status, 'PASS', 'desktop overflow is not a mobile defect');
  assert.deepEqual(judge({ ...base, viewport: 'mobile', audit: { ...clean, overflow: 40 } }).problems, ['yatay taşma 40px']);
  assert.deepEqual(judge({ ...base, audit: { ...clean, unnamedControls: 2, headings: 0 } }).problems, ['başlık yok', '2 adsız kontrol']);
});

test('slug and report line are stable', () => {
  assert.equal(slug('/properties/[id]/dues'), 'properties-id-dues');
  assert.equal(slug('/'), 'root');
  assert.equal(reportLine(1, { status: 'PASS', route: '/dues', viewport: 'mobile', action: 'Ekran açıldı', expected: 'hata yok', actual: 'hata yok', evidence: 'a.png' }),
    '1. [PASS] /dues (mobile) > Ekran açıldı. Beklenen: hata yok. Gerçek: hata yok. Kanıt: a.png');
});
