
import path from 'node:path';
import type {NextConfig} from 'next';
import createNextIntlPlugin from 'next-intl/plugin';

// The repository root is the build root so the web and mobile clients can share contract files under /shared.
const workspaceRoot=path.resolve(process.cwd(),'../..');
const config:NextConfig={output:'standalone',outputFileTracingRoot:workspaceRoot,poweredByHeader:false,devIndicators:false,allowedDevOrigins:['192.168.1.125'],turbopack:{root:workspaceRoot},async headers(){return [{source:'/(.*)',headers:[{key:'X-Content-Type-Options',value:'nosniff'},{key:'X-Frame-Options',value:'DENY'},{key:'Referrer-Policy',value:'no-referrer'},{key:'Permissions-Policy',value:'camera=(), microphone=(), geolocation=()'}]}];}};

export default createNextIntlPlugin()(config);
