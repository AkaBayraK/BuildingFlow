"use client";
import {useEffect,useState} from "react";
import {useAuthorizedApi} from "@/services/api";
import {initials} from "@/features/account/model";
// İŞ19: an account photo read with the signed-in token (never a public URL); initials while loading or when there is none.
export function AccountAvatar({path,name,className="size-10 text-sm"}:{path:string|null;name:string;className?:string}){
 const api=useAuthorizedApi();const [url,setUrl]=useState<string|null>(null);
 useEffect(()=>{if(!path)return;const c=new AbortController();let created:string|null=null;
  // A missing photo (404) is normal; the initials stay without an error message.
  void api<Blob>(path,{signal:c.signal},"blob",[403,404]).then(blob=>{if(c.signal.aborted)return;created=URL.createObjectURL(blob);setUrl(created);}).catch(()=>{if(!c.signal.aborted)setUrl(null);});
  return()=>{c.abort();if(created)URL.revokeObjectURL(created);setUrl(null);};},[api,path]);
 return <span className={"inline-flex shrink-0 items-center justify-center overflow-hidden rounded-full bg-primary/10 font-semibold text-primary "+className} aria-hidden="true">
  {url?<img src={url} alt="" className="size-full object-cover"/>:initials(name)}</span>;
}
