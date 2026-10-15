export const MAX_PHOTO_BYTES=256*1024;
export type Profile={userId:string;firstName:string;lastName:string;phone:string|null;photoId:string|null;photoSha256:string|null;version:number;modifiedAt:string|null};
export type Command={operationId:string;expectedVersion:number;firstName:string;lastName:string;phone:string|null;photoAction:'KEEP'|'REPLACE'|'REMOVE';photoBase64:string|null};
const uuid=/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
function object(value:unknown):Record<string,unknown>{if(!value || typeof value!=='object' || Array.isArray(value))throw Error('Geçersiz profil yanıtı.');return value as Record<string,unknown>;}
function name(value:unknown,empty=false):string {if(typeof value!=='string' || value.length>80 || /[\x00-\x1f\x7f]/.test(value) || (!empty&&!value.trim()))throw Error('Ad ve soyad en fazla80 karakter olmalı.');return value.trim();}
function phone(value:unknown):string|null {if(value===null || value==='')return null;if(typeof value!=='string')throw Error('Telefon geçersiz.');const p=value.trim();if(p.length>32 || !/^[+ ()0-9-]+$/.test(p) || p.replace(/[^0-9]/g,'').length<7 || p.replace(/[^0-9]/g,'').length>15)throw Error('Geçerli bir iletişim telefonu girin.');return p;}
export function parseProfile(value:unknown,userId:string):Profile {
 const p=object(value);
 if(p.userId!==userId || typeof p.version!=='number' || !Number.isSafeInteger(p.version) || p.version<0 || !(p.modifiedAt===null || typeof p.modifiedAt==='string'&&Number.isFinite(Date.parse(p.modifiedAt))))throw Error('Profil hesabı veya sürümü eşleşmiyor.');
 const id=p.photoId;if(!(id===null || typeof id==='string'&&uuid.test(id)&&!/^0{8}-0{4}-0{4}-0{4}-0{12}$/.test(id)))throw Error('Fotoğraf kimliği geçersiz.');
 const sha=p.photoSha256;if((id===null&&sha!==null) || (id!==null&&(typeof sha!=='string'||!/^[a-f0-9]{64}$/i.test(sha))))throw Error('Fotoğraf bütünlüğü geçersiz.');
 const first=name(p.firstName,p.version===0),last=name(p.lastName,p.version===0),tel=phone(p.phone);
 if(p.version===0 && (first || last || tel || id || p.modifiedAt!==null))throw Error('Başlangıç profili geçersiz.');
 if(p.version>0 && p.modifiedAt===null)throw Error('Profil güncelliği eksik.');
 return {userId,firstName:first,lastName:last,phone:tel,photoId:id===null?null:String(id).toLowerCase(),photoSha256:sha===null?null:String(sha).toLowerCase(),version:p.version,modifiedAt:p.modifiedAt===null?null:String(p.modifiedAt)};
}
export function parseCommand(value:unknown):Command {
 const c=object(value);if(typeof c.operationId!=='string' || !uuid.test(c.operationId) || /^0{8}-0{4}-0{4}-0{4}-0{12}$/.test(c.operationId) || typeof c.expectedVersion!=='number'||!Number.isSafeInteger(c.expectedVersion)||c.expectedVersion<0 || !['KEEP','REPLACE','REMOVE'].includes(String(c.photoAction)))throw Error('Geçersiz profil işlemi.');
 const action=c.photoAction as Command['photoAction'];const photo=c.photoBase64;
 if(action==='REPLACE'){if(typeof photo!=='string'||photo.length>350000 || base64Bytes(photo).length>MAX_PHOTO_BYTES || !photo.startsWith('/9j/'))throw Error('Fotoğraf JPEG biçiminde ve256KB altında olmalı.');}
 else if(photo!==null)throw Error('Fotoğraf alanı beklenmiyor.');
 return {operationId:c.operationId.toLowerCase(),expectedVersion:c.expectedVersion,firstName:name(c.firstName),lastName:name(c.lastName),phone:phone(c.phone),photoAction:action,photoBase64:photo===null?null:String(photo)};
}
export function parseReceipt(value:unknown,command:Command,user:string):Profile {
 const r=object(value);if(r.operationId!==command.operationId || typeof r.replayed!=='boolean')throw Error('İşlem makbuzu eşleşmiyor.');
 const p=parseProfile(r.profile,user);
 if(p.version!==command.expectedVersion+1 || p.firstName!==command.firstName || p.lastName!==command.lastName || p.phone!==command.phone || (command.photoAction==='REMOVE'&&p.photoId!==null) || (command.photoAction==='REPLACE'&&p.photoId===null))throw Error('Profil makbuzu geçersiz.');
 return p;
}
const alphabet='ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/';
export function base64Bytes(value:string):Uint8Array {
 if(!value || value.length%4 || !/^[A-Za-z0-9+/]+={0,2}$/.test(value))throw Error('Geçersiz fotoğraf kodlaması.');
 const pad=value.endsWith('==')?2:value.endsWith('=')?1:0;const out=new Uint8Array(value.length/4*3-pad);let offset=0;
 for(let i=0;i<value.length;i+=4){const a=alphabet.indexOf(value[i]),b=alphabet.indexOf(value[i+1]),c=value[i+2]==='='?0:alphabet.indexOf(value[i+2]),d=value[i+3]==='='?0:alphabet.indexOf(value[i+3]);const n=(a<<18)|(b<<12)|(c<<6)|d;if(offset<out.length)out[offset++]=n>>16;if(offset<out.length)out[offset++]=n>>8;if(offset<out.length)out[offset++]=n;}
 return out;
}
export function bytesBase64(bytes:Uint8Array):string {let out='';for(let i=0;i<bytes.length;i+=3){const a=bytes[i],b=bytes[i+1],c=bytes[i+2],n=(a<<16)|((b??0)<<8)|(c??0);out+=alphabet[(n>>18)&63]+alphabet[(n>>12)&63]+(b===undefined?'=':alphabet[(n>>6)&63])+(c===undefined?'=':alphabet[n&63]);}return out;}
export function utf8(value:string):Uint8Array {const encoded=encodeURIComponent(value),out:number[]=[];for(let i=0;i<encoded.length;i++){if(encoded[i]==='%'){out.push(parseInt(encoded.slice(i+1,i+3),16));i+=2;}else out.push(encoded.charCodeAt(i));}return new Uint8Array(out);}
export function text(bytes:Uint8Array):string {return decodeURIComponent(Array.from(bytes,b=>'%'+b.toString(16).padStart(2,'0')).join(''));}
