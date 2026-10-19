import 'server-only';
import {cookies} from 'next/headers';
import {apiOrigin} from './listings';
import {readSite} from './public-site';
export async function previewSite(){const token=(await cookies()).get('bf-website-preview')?.value;if(!token||token.length>512)return null;const response=await fetch(new URL('/api/public/v1/preview',apiOrigin()),{method:'POST',cache:'no-store',redirect:'error',headers:{'Content-Type':'application/json'},body:JSON.stringify({token}),signal:AbortSignal.timeout(8000)});if(response.status===404)return null;if(!response.ok)throw Error('Preview unavailable');const raw=await response.text();if(raw.length>2000000)throw Error('Preview too large');const value=JSON.parse(raw);if(typeof value.siteKey!=='string')throw Error('Invalid preview');return readSite(value,value.siteKey);}
