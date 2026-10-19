/** Public origins only. Credentials, fragments and query strings never enter CSP. */
export function publicOrigin(value: string, https: boolean): string {
 const u=new URL(value);
 if(!['https:','http:'].includes(u.protocol)||u.username||u.password||u.search||u.hash||(https&&u.protocol!=='https:'))throw new Error('Invalid public security origin');
 return u.origin;
}
export function contentSecurityPolicy(nonce:string,api:string,authority:string,https:boolean,website?:string):string {
 if(!/^[A-Za-z0-9+/]+={0,2}$/.test(nonce))throw new Error('Invalid CSP nonce');
 const origins=[...new Set([publicOrigin(api,https),publicOrigin(authority,https)])].join(' ');
 return ["default-src 'self'",`script-src 'self' 'nonce-${nonce}' 'strict-dynamic'`,"style-src 'self' 'unsafe-inline'","img-src 'self' blob: data:","font-src 'self'",`connect-src 'self' ${origins}`,`frame-src ${publicOrigin(authority,https)}`,"object-src 'none'","base-uri 'none'",`form-action 'self'${website?' '+publicOrigin(website,https):''}`,"frame-ancestors 'none'",...(https?['upgrade-insecure-requests']:[])].join('; ');
}
