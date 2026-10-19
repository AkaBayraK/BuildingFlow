import {categories,priorities} from '../sync/contracts.ts';
import {contextKey,type Context} from '../auth/contracts.ts';
export const managerRoles=['SUPER_ADMIN','SITE_ADMIN','MANAGER'];
export const sections=['overview','buildings','units','residents','finance','announcements','requests'] as const;
export type Section=typeof sections[number];
export type Row={id:string;title:string;detail:string;amount:string|null;state:string|null;category?:string|null;priority?:string|null;photoId?:string|null;photoSha256?:string|null};
export type Snapshot={userId:string;contextKey:string;asOf:string;propertyName:string;address:string;section:Section;page:number;total:number;summary:{buildings:number;units:number;residents:number;announcements:number;requests:number;balance:string;overdue:string};rows:Row[]};
const fail=()=>{throw Error('Yönetim yanıtı doğrulanamadı.');};
const text=(v:unknown,max:number):string=>typeof v==='string'&&v.length<=max?v:fail();
const count=(v:unknown):number=>typeof v==='number'&&Number.isSafeInteger(v)&&v>=0?v:fail();
const money=(v:unknown):string=>typeof v==='string'&&/^\d{1,24}\.\d{2}$/.test(v)?v:fail();
export function parseManager(value:unknown,user:string,c:Context,section:Section,page:number):Snapshot{
 if(!managerRoles.includes(c.role)||c.unitId!==null||!sections.includes(section)||!Number.isInteger(page)||page<1||page>10000)fail();
 if(!value||typeof value!=='object')fail();const v=value as Snapshot;
 if(v.userId!==user||v.contextKey!==contextKey(c)||v.section!==section||v.page!==page||!Number.isFinite(Date.parse(v.asOf)))fail();
 if(!v.summary||!Array.isArray(v.rows)||v.rows.length>50)fail();
 const s=v.summary;const summary={buildings:count(s.buildings),units:count(s.units),residents:count(s.residents),announcements:count(s.announcements),requests:count(s.requests),balance:money(s.balance),overdue:money(s.overdue)};
 if(BigInt(summary.overdue.replace('.',''))>BigInt(summary.balance.replace('.','')))fail();
 const total=count(v.total);if(v.rows.length!==Math.min(50,Math.max(0,total-(page-1)*50)))fail();
 const states:Record<Section,(string|null)[]>={overview:[null],buildings:[null],units:[null],residents:['OWNER','TENANT','RESIDENT'],finance:['CANCELLED','PAID','OVERDUE','OPEN'],announcements:['DRAFT','PUBLISHED','ARCHIVED'],requests:['RECORDED','IN_PROGRESS','RESOLVED','REJECTED']};
 const ids=new Set<string>();const rows=v.rows.map(r=>{if(!r||!/^[a-f0-9]{8}-(?:[a-f0-9]{4}-){3}[a-f0-9]{12}$/i.test(r.id)||ids.has(r.id)||!states[section].includes(r.state))fail();ids.add(r.id);if(section==='finance'?r.amount===null:r.amount!==null)fail();if(section==='requests'&&(r.category!=null&&!Object.hasOwn(categories,r.category)||r.priority!=null&&!Object.hasOwn(priorities,r.priority)||r.photoId!=null&&(!/^[a-f0-9-]{36}$/i.test(r.photoId)||!r.photoSha256||!/^[a-f0-9]{64}$/i.test(r.photoSha256))))fail();return {category:r.category??null,priority:r.priority??null,photoId:r.photoId??null,photoSha256:r.photoSha256??null,id:r.id,title:text(r.title,500),detail:text(r.detail,10000),amount:r.amount===null?null:money(r.amount),state:r.state};});
 return {userId:user,contextKey:contextKey(c),asOf:text(v.asOf,50),propertyName:text(v.propertyName,500),address:text(v.address,1000),section,page,total,summary,rows};
}
