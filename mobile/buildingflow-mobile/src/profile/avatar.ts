import {useEffect,useState} from 'react';
import {Platform} from 'react-native';
import {useOptionalAuth} from '../auth/AuthProvider';
import {cachedProfile,showPhoto} from './outbox';
import {parseProfile} from './contracts';
import {familyName,givenName} from './names';
type Entry={user:string;uri:string|null;name:string|null};
let current:Entry|null=null;const listeners=new Set<()=>void>();const loading=new Set<string>();
// The profile screen publishes the decrypted photo; headers on every tab read the same value instead of decrypting again.
export function publishAvatar(user:string,uri:string|null,name?:string|null){current={user,uri,name:name===undefined?(current?.user===user?current.name:null):name};listeners.forEach(l=>l());}
// Home greeting shows "Ad SOYAD" in the same form the name rule stores it.
export const fullName=(p:{firstName:string;lastName:string}|null|undefined)=>p?[givenName(p.firstName),familyName(p.lastName)].filter(Boolean).join(' ')||null:null;
export function useAvatar():{uri:string|null;name:string|null}{
 const auth=useOptionalAuth();const user=auth?.session?.userId??null;const [,tick]=useState(0);
 useEffect(()=>{const l=()=>tick(v=>v+1);listeners.add(l);return()=>{listeners.delete(l);};},[]);
 useEffect(()=>{
  if(!auth||!user||Platform.OS==='web'||current?.user===user||loading.has(user))return;
  loading.add(user);
  void auth.profileCache(async(db,files)=>{const c=await cachedProfile(db,user);const visible=await showPhoto(db,user);const photo=visible&&c?.photoFile?await files.read(c.photoFile):null;let name=fullName(c?.profile);
   // Right after sign-in nothing is cached yet; read the name from the centre so the greeting is not the login name.
   if(!name&&!auth.offline){try{const r=await auth.profileRequest('/api/v1/mobile/profile');if(r.ok)name=fullName(parseProfile(await r.json(),user));}catch{/* The login name stays until the profile is available. */}}
   return {uri:photo?'data:image/jpeg;base64,'+photo:null,name};})
   .then(v=>{if(current?.user!==user)publishAvatar(user,v.uri,v.name);}).catch(()=>{}).finally(()=>loading.delete(user));
 },[auth,user]);
 return current&&current.user===user?{uri:current.uri,name:current.name}:{uri:null,name:null};
}
