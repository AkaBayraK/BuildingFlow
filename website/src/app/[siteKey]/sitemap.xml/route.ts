export const dynamic='force-dynamic';
import {sitemap} from '../../../lib/sitemap';
export async function GET(_request:Request,{params}:{params:Promise<{siteKey:string}>}){return new Response(await sitemap((await params).siteKey),{headers:{'Content-Type':'application/xml','Cache-Control':'no-store'}});}
