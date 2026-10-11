"use server";
import {cookies} from "next/headers";
import {isLocale,localeCookie,localeCookieOptions} from "./locale";
export async function setLocale(value: string) {
  if(!isLocale(value)) throw new Error("Unsupported locale");
  (await cookies()).set(localeCookie,value,localeCookieOptions(process.env.NODE_ENV==="production"));
}
