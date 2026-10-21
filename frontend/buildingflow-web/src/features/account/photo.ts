// Browser-side photo preparation (no package): centre-crop to a square, 512 px JPEG under the API's 256 KB limit.
// Re-encoding through a canvas also drops EXIF/location data before the file leaves the device.
export const photoTypes=["image/jpeg","image/png","image/webp"];
export const maxSourceBytes=20*1024*1024;
const maxBytes=256*1024;
export class PhotoError extends Error{readonly reason:"type"|"size"|"decode"|"compress";constructor(reason:PhotoError["reason"]){super(reason);this.reason=reason;}}
function base64(bytes:Uint8Array){let s="";for(let i=0;i<bytes.length;i+=0x8000)s+=String.fromCharCode(...bytes.subarray(i,i+0x8000));return btoa(s);}
export async function preparePhoto(file:File):Promise<{base64:string;previewUrl:string}>{
 if(!photoTypes.includes(file.type))throw new PhotoError("type");
 if(file.size<1||file.size>maxSourceBytes)throw new PhotoError("size");
 let image:ImageBitmap;try{image=await createImageBitmap(file);}catch{throw new PhotoError("decode");}
 try{
  const side=Math.min(image.width,image.height);if(side<1)throw new PhotoError("decode");
  if(!Number.isFinite(image.width)||!Number.isFinite(image.height)||image.width<1||image.height<1||image.width*image.height>40_000_000)throw new PhotoError("size");
  const canvas=document.createElement("canvas");canvas.width=512;canvas.height=512;const ctx=canvas.getContext("2d");if(!ctx)throw new PhotoError("decode");
  ctx.fillStyle="#fff";ctx.fillRect(0,0,512,512);ctx.drawImage(image,(image.width-side)/2,(image.height-side)/2,side,side,0,0,512,512);
  for(const quality of [0.85,0.75,0.65,0.5]){
   const blob=await new Promise<Blob|null>(resolve=>canvas.toBlob(resolve,"image/jpeg",quality));
   if(blob&&blob.size<=maxBytes)return {base64:base64(new Uint8Array(await blob.arrayBuffer())),previewUrl:URL.createObjectURL(blob)};
  }
  throw new PhotoError("compress");
 }finally{image.close();}
}
export async function sha256Hex(value:string){const digest=await crypto.subtle.digest("SHA-256",new TextEncoder().encode(value));return Array.from(new Uint8Array(digest),b=>b.toString(16).padStart(2,"0")).join("");}
