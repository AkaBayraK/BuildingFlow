
import type {NextConfig} from 'next';
import createNextIntlPlugin from 'next-intl/plugin';

const config:NextConfig={output:'standalone',poweredByHeader:false,devIndicators:false,allowedDevOrigins:['192.168.1.125'],turbopack:{root:process.cwd()},async headers(){return [{source:'/(.*)',headers:[{key:'X-Content-Type-Options',value:'nosniff'},{key:'X-Frame-Options',value:'DENY'},{key:'Referrer-Policy',value:'no-referrer'},{key:'Permissions-Policy',value:'camera=(), microphone=(), geolocation=()'}]}];}};

export default createNextIntlPlugin()(config);
