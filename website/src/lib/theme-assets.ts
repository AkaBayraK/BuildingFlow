// Local, site-key scoped assets. CMS media integration belongs to WEBSITE FAZ 05.
// Only project-owned or explicitly authorized images may be added under public/images/.
export type ThemeAssets={hero?:string;logo?:string};
export const siteAssets:Record<string,ThemeAssets>={};
export function themeAssets(siteKey:string,preview=false):ThemeAssets{
 const assets=preview?{hero:'/images/residence.webp'}:siteAssets[siteKey]??{};
 const local=(value:string|undefined)=>value&&/^\/images\/[a-z0-9-]+\.(webp|png|jpg|svg)$/.test(value)?value:undefined;
 return {hero:local(assets.hero),logo:local(assets.logo)};
}

export function managementHref(origin:string|undefined){try{const url=new URL(origin??'http://localhost:3003');if(!['http:','https:'].includes(url.protocol)||url.username||url.password||url.pathname!=='/'||url.search||url.hash)return null;return new URL('/login',url).href}catch{return null}}
