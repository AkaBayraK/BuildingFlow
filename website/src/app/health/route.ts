export const dynamic='force-dynamic';
export function GET(){return new Response(null,{status:204,headers:{'Cache-Control':'no-store','X-Robots-Tag':'noindex, nofollow'}});}
