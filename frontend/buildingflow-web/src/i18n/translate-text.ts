import {createTranslator} from "next-intl";
import tr from "../../messages/tr.json" with {type:"json"};
import en from "../../messages/en.json" with {type:"json"};
import {resolveLocale} from "./locale.ts";
const catalogs={tr,en};
const templates=["requiredField","maxCharacters","fieldMaxCharacters","entitySaved","entityDeleted","propertyCreated"];
const escape=(text:string)=>text.replace(/[.*+?^${}()|[\]\\]/g,"\\$&");
export function translateUiText(text:string,locale:string):string{
 const target=resolveLocale(locale);
 for(const catalog of Object.values(catalogs)){
  for(const namespace of ["ui","errors","dues","payments","expenses","communication","apiErrors","apiFields"] as const){const entries:Record<string,string>=catalog[namespace];for(const [key,value]of Object.entries(entries))if(!value.includes("{")&&value===text)return (catalogs[target][namespace] as Record<string,string>)[key]??text;}
  for(const [namespace,keys] of [["errors",["requiredMax"]],["apiErrors",["BF_FIELD_REQUIRED_MAX","BF_FIELD_REQUIRED","BF_FIELD_TOO_LONG"]],["ui",templates]] as const)for(const key of keys){const source=(catalog[namespace] as Record<string,string>)[key];const args=[...source.matchAll(/\{(\w+)\}/g)].map(match=>match[1]);const pattern=escape(source).replace(/\\\{\w+\\\}/g,"(.+?)");const match=new RegExp("^"+pattern+"$").exec(text);
   if(match){const values:Record<string,string|number>={};args.forEach((name,index)=>{const value=match[index+1];values[name]=name==="max"?Number(value):(name==="field"||name==="entity")?translateUiText(value,target):value;});return createTranslator({locale:target,messages:catalogs[target][namespace] as Record<string,string>})(key,values);}
  }
 }
 return text;
}
