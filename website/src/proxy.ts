import {NextRequest,NextResponse} from 'next/server';
import {origin,requestHost,resolveHost} from './lib/routing';
export async function proxy(request:NextRequest){
 const host=requestHost(request.headers.get('host'));let platform:URL;try{platform=origin()}catch{return new NextResponse(null,{status:503})}if(!host)return new NextResponse(null,{status:404});
 const headers=new Headers(request.headers);for(const key of ['x-bf-site-key','x-bf-domain','x-nonce','content-security-policy','x-forwarded-host','x-forwarded-proto'])headers.delete(key);
 const nonce=Buffer.from(crypto.randomUUID()).toString('base64');const csp=`default-src 'self'; script-src 'self' 'nonce-${nonce}'${process.env.NODE_ENV==='production'?" 'strict-dynamic'":" 'unsafe-eval'"}; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self'; connect-src 'self'; base-uri 'none'; frame-ancestors 'none'; form-action 'self'`;headers.set('x-nonce',nonce);headers.set('Content-Security-Policy',csp);
 let response:NextResponse;const path=request.nextUrl.pathname;
 if(host===platform.host){response=NextResponse.next({request:{headers}})}else{
  let domain;try{domain=await resolveHost(host,process.env.WEBSITE_API_ORIGIN??'http://localhost:5005')}catch{return new NextResponse(null,{status:503,headers:{'Cache-Control':'no-store'}})}if(!domain)return new NextResponse(null,{status:404,headers:{'Cache-Control':'no-store'}});
  headers.set('x-bf-site-key',domain.siteKey);headers.set('x-bf-domain',domain.hostName);
  if(path==='/preview'||path.startsWith('/preview/'))return new NextResponse(null,{status:404});
  if(path==='/'+domain.siteKey||path.startsWith('/'+domain.siteKey+'/')){const u=new URL('https://'+domain.hostName);u.pathname=path.slice(domain.siteKey.length+1)||'/';u.search=request.nextUrl.search;response=NextResponse.redirect(u,308)}
  else if(path.startsWith('/media/')||path.startsWith('/inquiries/')){const expected=path.startsWith('/media/')?'/media/':'/inquiries/';if(path.split('/')[2]!==domain.siteKey)return new NextResponse(null,{status:404});response=NextResponse.next({request:{headers}})}
  else if(path==='/robots.txt'||path==='/sitemap.xml'){response=NextResponse.next({request:{headers}})}else{const u=request.nextUrl.clone();u.pathname='/'+domain.siteKey+(path==='/'?'':path);response=NextResponse.rewrite(u,{request:{headers}})}
 }
 response.headers.set('Content-Security-Policy',csp);response.headers.set('Cache-Control','private, no-store');if(platform.protocol==='https:'||host!==platform.host)response.headers.set('Strict-Transport-Security','max-age=15552000');if(path.startsWith('/preview'))response.headers.set('X-Robots-Tag','noindex, nofollow');return response;
}
export const config={matcher:['/((?!_next/static|_next/image|favicon.ico).*)']};
