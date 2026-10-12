"use server";
import {cookies} from "next/headers";
import {isLocale,localeCookie,localeCookieOptions,loginLocaleCookie} from "./locale";
export async function setLocale(value: string) {
  if(!isLocale(value)) throw new Error("Unsupported locale");
  const store=await cookies();const options=localeCookieOptions(process.env.NODE_ENV==="production");
  store.set(localeCookie,value,options);
  store.set(loginLocaleCookie,value,{...options,httpOnly:false});
}
