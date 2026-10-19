import {Directory,File,Paths} from 'expo-file-system';
import * as SecureStore from 'expo-secure-store';
import {AESEncryptionKey,AESSealedData,aesEncryptAsync,aesDecryptAsync} from 'expo-crypto';
import {utf8,text} from './contracts';
import type {PrivateFiles,PrivateIdentity} from './files';
function directory(identity:PrivateIdentity){return new Directory(Paths.document,'profile-'+identity.name);}
function valid(name:string){if(!/^(job|avatar|request-draft|request-job|request-photo)-[0-9a-f-]{36}\.enc$/.test(name))throw Error('Geçersiz özel dosya.');}
export function privateFiles(identity:PrivateIdentity):PrivateFiles {
 async function key(){const hex=await SecureStore.getItemAsync(identity.keyId);if(!hex)throw Error('Özel dosya anahtarı yok.');return AESEncryptionKey.import(hex,'hex');}
 return {
  async write(name,value){valid(name);const dir=directory(identity);dir.create({idempotent:true,intermediates:true});const sealed=await aesEncryptAsync(utf8(value),await key(),{additionalData:utf8(identity.name+'|'+name)});const file=new File(dir,name);file.create({overwrite:true});file.write(await sealed.combined());},
  async read(name){valid(name);const file=new File(directory(identity),name);if(!file.exists || file.size>600000)throw Error('Özel taslak bulunamadı.');const bytes=await aesDecryptAsync(AESSealedData.fromCombined(await file.bytes()),await key(),{additionalData:utf8(identity.name+'|'+name)});return text(bytes);},
  async drop(name){valid(name);const file=new File(directory(identity),name);if(file.exists)file.delete();}
 };
}
export function clearPrivateFiles(name:string){const dir=directory({name,keyId:''});if(dir.exists)dir.delete();}
