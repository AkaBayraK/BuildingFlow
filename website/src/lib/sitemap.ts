import 'server-only';
import {getSite} from './site';
import {canonical} from './seo';
const xml=(s:string)=>s.replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;');
export async function sitemap(key:string){const site=await getSite(key);const paths=site.isDemo?[]:site.pages.map(p=>p.slug?'/'+p.slug:'/');if(!site.isDemo)paths.push('/ilanlar');const urls=await Promise.all(paths.map(p=>canonical(site,p)));return '<?xml version="1.0" encoding="UTF-8"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">'+urls.map(u=>'<url><loc>'+xml(u)+'</loc></url>').join('')+'</urlset>';}
