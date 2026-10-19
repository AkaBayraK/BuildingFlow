export const dynamic='force-dynamic';
import {headers} from 'next/headers';
import {sitemap} from '../../lib/sitemap';
export async function GET(){const key=(await headers()).get('x-bf-site-key');return new Response(key?await sitemap(key):'<?xml version="1.0"?><urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9"/>',{headers:{'Content-Type':'application/xml','Cache-Control':'no-store'}});}
