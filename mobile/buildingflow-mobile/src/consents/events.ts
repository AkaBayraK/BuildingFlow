import * as Crypto from 'expo-crypto';
// Request sync (AuthProvider) and the consent screens flush the same queue; this tells the screens to re-read the local copy.
const listeners=new Set<()=>void>();
export function onConsentChange(listener:()=>void){listeners.add(listener);return()=>{listeners.delete(listener);};}
export function consentChanged(){listeners.forEach(l=>l());}
// SHA-256 of the UTF-8 body, compared with the published bodySha256 before a text is shown for approval.
export const digestText=(text:string)=>Crypto.digestStringAsync(Crypto.CryptoDigestAlgorithm.SHA256,text,{encoding:Crypto.CryptoEncoding.HEX});
