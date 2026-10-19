// Local, site-key scoped assets. CMS media integration belongs to WEBSITE FAZ 05.
// Only project-owned or explicitly authorized images may be added under public/images/.
export type ThemeAssets={hero?:string;logo?:string};
export const siteAssets:Record<string,ThemeAssets>={};
export function themeAssets(siteKey:string,preview=false):ThemeAssets{
 const assets=preview?{hero:'/images/residence.webp'}:siteAssets[siteKey]??{};
 const local=(value:string|undefined)=>value&&/^\/images\/[a-z0-9-]+\.(webp|png|jpg|svg)$/.test(value)?value:undefined;
 return {hero:local(assets.hero),logo:local(assets.logo)};
}

export function managementHref(origin:string|undefined,siteKey?:string,audience:'all'|'management'|'resident'='all'){
 try{const url=new URL(origin??'http://localhost:3003');if(!['http:','https:'].includes(url.protocol)||url.username||url.password||url.pathname!=='/'||url.search||url.hash||url.protocol==='http:'&&!['localhost','127.0.0.1'].includes(url.hostname))return null;
 if(siteKey!==undefined&&!/^[a-z0-9](?:[a-z0-9-]{1,61}[a-z0-9])$(?![\s\S])/.test(siteKey)||!['all','management','resident'].includes(audience))return null;
 return new URL(siteKey?`/website-entry/${siteKey}/${audience}`:'/',url).href}catch{return null}
}
