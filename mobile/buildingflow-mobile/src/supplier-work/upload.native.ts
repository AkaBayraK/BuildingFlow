import {File,Paths} from 'expo-file-system';
import * as Crypto from 'expo-crypto';
import {base64Bytes} from '../profile/contracts';
// The decrypted photo exists only as a short-lived cache file while the multipart request is built and sent.
export function supplierPhotoForm(photo:string,fields:Record<string,unknown>){
 const file=new File(Paths.cache,'supplier-upload-'+Crypto.randomUUID()+'.jpg');file.create({overwrite:true});file.write(base64Bytes(photo));
 const data=new FormData();
 data.append('file',{uri:file.uri,name:'evidence.jpg',type:'image/jpeg'} as unknown as Blob);
 for(const key of ['operationId','version','caption'])data.append(key,String(fields[key]));
 return {data,dispose:()=>{try{if(file.exists)file.delete();}catch{/* Cache cleanup is best effort; the OS also purges it. */}}};
}
