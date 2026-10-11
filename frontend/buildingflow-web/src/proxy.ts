import {NextRequest,NextResponse} from 'next/server';
import {contentSecurityPolicy} from './security/policy';
export function proxy(request:NextRequest) {
 // Production CSP uses a fresh request nonce, never a client-supplied CSP/nonce.
 if(process.env.NODE_ENV!=='production')return NextResponse.next();
 const nonce=Buffer.from(crypto.randomUUID()).toString('base64');
 const csp=contentSecurityPolicy(nonce,process.env.NEXT_PUBLIC_API_URL??'http://localhost:5005',process.env.NEXT_PUBLIC_OIDC_AUTHORITY??'http://localhost:8080/realms/buildingflow',process.env.BUILDINGFLOW_PUBLIC_HTTPS==='true');
 const headers=new Headers(request.headers);headers.set('x-nonce',nonce);headers.set('Content-Security-Policy',csp);
 const response=NextResponse.next({request:{headers}});response.headers.set('Content-Security-Policy',csp);response.headers.set('Cache-Control','private, no-store');return response;
}
export const config={matcher:['/((?!_next/static|_next/image|favicon.ico|health).*)']};
