import {cookies} from 'next/headers';
import {previewSite} from '../../../../../../lib/preview';
import {createHash} from 'node:crypto';
import {validKey,uuid} from '../../../../../../lib/public-site';
export async function GET(_request:Request,{params}:{params:Promise<{siteKey:string;id:string;sha:string;ext:string}>}){
 const {siteKey,id,sha,ext}=await params;
 if(!validKey(siteKey)||!uuid(id)||!/^([a-f0-9]{64})$/.test(sha)||!['jpg','png'].includes(ext))return new Response(null,{status:404});
 try{const origin=new URL(process.env.WEBSITE_API_ORIGIN??'http://localhost:5005');if(!['http:','https:'].includes(origin.protocol)||origin.username||origin.password||origin.pathname!=='/'||origin.search||origin.hash)throw Error('Invalid origin');
 let upstream=await fetch(new URL(`/api/public/v1/sites/${siteKey}/media/${id}/${sha}.${ext}`,origin),{cache:'no-store',redirect:'error',signal:AbortSignal.timeout(8000)});
 if(upstream.status===404){const token=(await cookies()).get('bf-website-preview')?.value;if(token){const site=await previewSite();if(site?.siteKey===siteKey)upstream=await fetch(new URL(`/api/public/v1/preview/media/${id}/${sha}`,origin),{method:'POST',cache:'no-store',redirect:'error',signal:AbortSignal.timeout(8000),headers:{'Content-Type':'application/json'},body:JSON.stringify({token})})}if(upstream.status===404)return new Response(null,{status:404});}if(!upstream.ok)throw Error('Upstream unavailable');
 const mime=ext==='jpg'?'image/jpeg':'image/png';if(upstream.headers.get('content-type')?.split(';')[0]!==mime)throw Error('Invalid content');
 const reader=upstream.body?.getReader();if(!reader)throw Error('Empty image');const chunks:Uint8Array[]=[];let size=0;try{while(true){const {done,value}=await reader.read();if(done)break;size+=value.length;if(size>5242880)throw Error('Image exceeds bounds');chunks.push(value)}}finally{await reader.cancel()}
 const bytes=Buffer.concat(chunks);if(!size||createHash('sha256').update(bytes).digest('hex')!==sha)throw Error('Integrity failure');
 return new Response(bytes,{headers:{'Content-Type':mime,'Cache-Control':'no-store','X-Content-Type-Options':'nosniff','Content-Disposition':'inline'}});
 }catch{return new Response(null,{status:502,headers:{'Cache-Control':'no-store'}})}
}
