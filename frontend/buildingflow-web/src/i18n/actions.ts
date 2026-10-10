"use server";
import {cookies} from "next/headers";
import {isLocale,localeCookie} from "./locale";
export async function setLocale(value: string) {
  if(!isLocale(value)) throw new Error("Unsupported locale");
  (await cookies()).set(localeCookie,value,{path:"/",maxAge:31536000,sameSite:"lax",httpOnly:true,secure:process.env.NODE_ENV==="production"});
}
