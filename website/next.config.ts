import type { NextConfig } from 'next';
import path from 'node:path';
const config: NextConfig = { output:'standalone', outputFileTracingRoot:path.resolve(process.cwd(),'..'), poweredByHeader: false, htmlLimitedBots: /.*/, devIndicators: false, turbopack: { root: path.resolve(process.cwd(),'..') }, async headers() { return [{ source: '/(.*)', headers: [{key:'X-Content-Type-Options',value:'nosniff'},{key:'X-Frame-Options',value:'DENY'},{key:'Referrer-Policy',value:'no-referrer'},{key:'Cache-Control',value:'no-store'},{key:'Permissions-Policy',value:'camera=(), microphone=(), geolocation=()'}] }]; } };
export default config;
