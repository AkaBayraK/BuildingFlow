import {base64Bytes} from '../profile/contracts';
// Browser preview: no device file is written; the photo is sent straight from memory.
export function supplierPhotoForm(photo:string,fields:Record<string,unknown>){
 const data=new FormData();
 data.append('file',new Blob([base64Bytes(photo).slice().buffer],{type:'image/jpeg'}),'evidence.jpg');
 for(const key of ['operationId','version','caption'])data.append(key,String(fields[key]));
 return {data,dispose:()=>{}};
}
