import {readContentPage,readDocument,type DocumentRecord} from "../communication/model.ts";
export const bannerName="buildingflow-dashboard-banner.jpg";
export function latestBanner(value:unknown,propertyId:string):DocumentRecord|null{
 const rows=readContentPage(value,row=>readDocument(row,propertyId)).items;
 return rows.filter(r=>r.fileName===bannerName&&r.announcementId===null&&r.audience==="ALL_MEMBERS"&&r.contentType==="image/jpeg").sort((a,b)=>Date.parse(b.createdAt)-Date.parse(a.createdAt)||(a.id<b.id?-1:a.id>b.id?1:0))[0]??null;
}
export function bannerFileValid(file:Pick<File,"size"|"type">){return file.size>0&&file.size<=10*1024*1024&&["image/jpeg","image/png","image/webp"].includes(file.type);}
export async function prepareBanner(file:File){
 if(!bannerFileValid(file))throw new Error("Invalid banner file");
 const image=await createImageBitmap(file);
 try{
  if(image.width<400||image.height<160||image.width>8192||image.height>8192||image.width*image.height>32000000)throw new Error("Invalid banner dimensions");
  const scale=Math.min(1,2560/image.width,1440/image.height),canvas=document.createElement("canvas");canvas.width=Math.round(image.width*scale);canvas.height=Math.round(image.height*scale);
  const context=canvas.getContext("2d");if(!context)throw new Error("Canvas unavailable");context.fillStyle="#fff";context.fillRect(0,0,canvas.width,canvas.height);context.drawImage(image,0,0,canvas.width,canvas.height);
  const blob=await new Promise<Blob|null>(resolve=>canvas.toBlob(resolve,"image/jpeg",.88));if(!blob||blob.size>10*1024*1024)throw new Error("Invalid prepared banner");
  return new File([blob],bannerName,{type:"image/jpeg"});
 }finally{image.close();}
}
