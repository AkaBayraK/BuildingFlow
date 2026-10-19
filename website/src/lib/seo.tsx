import 'server-only';
import {headers} from 'next/headers';
import {apiOrigin} from './listings';
import {canonicalPath,safeJson} from './routing';
import {validKey,type Site,type Page} from './public-site';
export async function primaryDomain(key:string){if(!validKey(key))return null;const r=await fetch(new URL(`/api/public/v1/sites/${key}/primary-domain`,apiOrigin()),{cache:'no-store',redirect:'error',signal:AbortSignal.timeout(3000)});if(r.status===404)return null;if(!r.ok)throw Error('Canonical origin unavailable');const v:unknown=await r.json();if(!v||typeof v!=='object'||!('hostName'in v)||typeof v.hostName!=='string')throw Error('Invalid canonical origin');return v.hostName;}
export async function canonical(site:Site,path='/'){return canonicalPath(site.siteKey,path,await primaryDomain(site.siteKey));}
export async function websiteMetadata(site:Site,page?:Page,path='/'){const url=await canonical(site,path);return {title:page?.seoTitle??`${page?.title??site.displayName} · ${site.displayName}`,description:page?.seoDescription??site.tagline??undefined,alternates:{canonical:url},robots:{index:!site.isDemo&&url.startsWith('https:'),follow:!site.isDemo},openGraph:{title:page?.seoTitle??page?.title??site.displayName,url,type:'website' as const,locale:'tr_TR'}};}
export async function Structured({site,path='/',extra}:{site:Site;path?:string;extra?:Record<string,unknown>}){const url=await canonical(site,path);const nonce=(await headers()).get('x-nonce')??undefined;return <script type="application/ld+json" nonce={nonce} children={safeJson(extra?{'@context':'https://schema.org',...extra,url}:{'@context':'https://schema.org','@type':path==='/'?'WebSite':'WebPage',name:site.displayName,url,description:site.tagline??undefined})}/>;}
