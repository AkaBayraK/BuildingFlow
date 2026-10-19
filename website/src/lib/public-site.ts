export const validKey = (key: string) => /^[a-z0-9](?:[a-z0-9-]{1,61}[a-z0-9])$/.test(key);
export const validSlug = (slug: string) => /^[a-z0-9](?:[a-z0-9-]{0,58}[a-z0-9])?$/.test(slug);
export type Section = {type:string; data:Record<string, unknown>};
export type Page = {kind:string;slug:string;title:string;menuLabel:string|null;showInMenu:boolean;sortOrder:number;seoTitle:string|null;seoDescription:string|null;sections:Section[]};
export type PublicMedia={id:string;contentType:string;width:number;height:number;sha256:string;altText:string};
export type Site = {siteKey:string;displayName:string;tagline:string|null;contactEmail:string|null;contactPhone:string|null;isDemo:boolean;theme:{template:string;primaryColor:string;accentColor:string;logoMediaId?:string|null;heroMediaId?:string|null};media?:PublicMedia[]|null;pages:Page[]};
const object = (v:unknown): v is Record<string,unknown> => !!v && typeof v === 'object' && !Array.isArray(v);
const text = (v:unknown, max=8000): v is string => typeof v==='string' && v.length<=max;
const optional = (v:unknown) => v===null || text(v);
const types = ['HERO','FEATURES','LISTINGS','NOTICES','AMENITIES','GALLERY','STATS','CONTACT','LOGIN_CARD','RICH_TEXT','VIDEO_LINK'];
export function readSite(value:unknown,key:string):Site {
 if (!object(value) || value.siteKey!==key || !validKey(key) || !text(value.displayName,120) || !optional(value.tagline) || !optional(value.contactEmail) || !optional(value.contactPhone) || typeof value.isDemo!=='boolean' || !object(value.theme) || value.theme.template!=='PREMIUM_RESIDENCE' || ![value.theme.primaryColor,value.theme.accentColor].every(c=>typeof c==='string' && /^#[0-9a-f]{6}$/i.test(c)) || !Array.isArray(value.pages) || value.pages.length>100) throw Error('Invalid public site');
 if(value.media!==null&&value.media!==undefined){if(!Array.isArray(value.media)||value.media.length>100)throw Error('Invalid public media');for(const m of value.media)if(!object(m)||typeof m.id!=='string'||!uuid(m.id)||!['image/jpeg','image/png'].includes(String(m.contentType))||!Number.isInteger(m.width)||Number(m.width)<1||Number(m.width)>4096||!Number.isInteger(m.height)||Number(m.height)<1||Number(m.height)>4096||typeof m.sha256!=='string'||!/^[a-f0-9]{64}$/.test(m.sha256)||!text(m.altText,200))throw Error('Invalid public media');}
 for(const id of [value.theme.logoMediaId,value.theme.heroMediaId])if(id!==null&&id!==undefined&&(typeof id!=='string'||!uuid(id)))throw Error('Invalid theme media');
 const slugs=new Set<string>();
 for(const p of value.pages) {
  if(!object(p) || !text(p.slug,60) || (p.slug!=='' && !validSlug(p.slug)) || slugs.has(p.slug) || !text(p.kind,30) || !text(p.title,120) || !optional(p.menuLabel) || !optional(p.seoTitle) || !optional(p.seoDescription) || typeof p.showInMenu!=='boolean' || !Number.isInteger(p.sortOrder) || !Array.isArray(p.sections) || p.sections.length>30) throw Error('Invalid public page');
  slugs.add(p.slug);
  for(const s of p.sections) if(!object(s) || !types.includes(String(s.type)) || !object(s.data) || JSON.stringify(s.data).length>16384) throw Error('Invalid public section');
 }
 if(!slugs.has('')) throw Error('Missing home');
 return value as Site;
}
// Local content links always stay under the selected site. No protocol-relative or executable URLs.
export function safeHref(value:unknown,key:string):string|null {
 if(typeof value!=='string' || /[\\\s\u0000-\u001f]/.test(value)) return null;
 if(/^#[a-zA-Z0-9_-]+$/.test(value)) return value;
 if(value==='/' ) return '/'+key;
 if(/^\/[a-z0-9-]+$/.test(value)) return '/'+key+value;
 try { const u=new URL(value); return u.protocol==='https:' && !u.username && !u.password ? u.href : null; } catch { return null; }
}
export async function fetchSite(key:string,origin:string,request:typeof fetch=fetch):Promise<Site|null> {
 if(!validKey(key)) return null;
 const base=new URL(origin);
 if(!['http:','https:'].includes(base.protocol) || base.username || base.password || base.pathname!=='/' || base.search || base.hash) throw Error('Invalid API origin');
 const response=await request(new URL('/api/public/v1/sites/'+key,base),{cache:'no-store',redirect:'error',signal:AbortSignal.timeout(8000),headers:{Accept:'application/json'}});
 if(response.status===404) return null;
 if(!response.ok) throw Error('Public site unavailable');
 const raw=await response.text(); if(raw.length>2_000_000) throw Error('Invalid public response');
 return readSite(JSON.parse(raw),key);
}

export const uuid=(value:string)=>/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(value);
export function publicMedia(site:Site,id:unknown){if(typeof id!=='string')return null;const m=site.media?.find(m=>m.id===id);return m?{...m,src:`/media/${site.siteKey}/${m.id}/${m.sha256}/${m.contentType==='image/jpeg'?'jpg':'png'}`}:null}
