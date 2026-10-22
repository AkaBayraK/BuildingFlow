import {createTranslator} from "next-intl";
import tr from "../../messages/tr.json" with {type:"json"};
import en from "../../messages/en.json" with {type:"json"};
import {resolveLocale} from "../i18n/locale.ts";
import {errorCodes} from "./error-codes.ts";
import {knownProblems} from "./known-problems.ts";
const fields=new Set(["name","city","district","address","floorCount","number","floor","grossSquareMeters","firstName","lastName","phone","phoneNumber","email","type","isEnabled","version","unitId","personId","startDate","endDate","isPrimaryContact","requestId","unitIds","period","amount","currency","dueDate","description","dueFrom","dueTo","status","reason","paidDate","method","reference","allocations","from","to","debtorType","partyRole","reasonId","cancellationApprovals","page","pageSize","search","sortBy","sortDirection","category","occurredDate","documentReference","explanation","content","audience","file","announcementId"]);
function record(value:unknown):value is Record<string,unknown>{return value!==null && typeof value==="object" && !Array.isArray(value);}
export function translateProblemText(text:string,locale:string,fallback="errors.invalidField") {
 const catalog=resolveLocale(locale)==="en"?en:tr;
 const messages:Record<string,string>={};for(const [namespace,entries]of Object.entries(catalog))for(const [key,value]of Object.entries(entries))messages[namespace+"_"+key]=value;
 const t=createTranslator({locale:resolveLocale(locale),messages});
 const key=Object.hasOwn(knownProblems,text)?knownProblems[text]:undefined;
 if(key)return t(key.replace(".","_"));
 const labels:Record<string,string>={"Yönetim alanı adı":"propertyName","Şehir":"city","İlçe":"district","Adres":"address","Bina/blok adı":"buildingBlockName","Blok adı":"buildingBlockName","Daire numarası":"apartmentNumber","Ad":"firstName","Soyad":"lastName","Telefon":"phone"};
 const match=/^(.+?) (zorunludur(?: ve en fazla (\d{1,3}) karakter olabilir)?|en fazla (\d{1,3}) karakter olabilir)\.$/.exec(text);
 if(match&&Object.hasOwn(labels,match[1])){
  const field=t("ui_"+labels[match[1]]);const max=Number(match[3]??match[4]);
  return t(match[3]?"errors_requiredMax":match[4]?"ui_fieldMaxCharacters":"ui_requiredField",{field,max});
 }
 return t(fallback.replace(".","_"));
}
export type CodedMessage={code:keyof typeof errorCodes;parameters:Record<string,string|number>};
export type ProblemFeedback={message:string;errors:Record<string,string[]>;code:keyof typeof errorCodes|null;parameters:Record<string,string|number>;severity:"error"|"warning"};
const fieldLabels=new Set(Object.keys(tr.apiFields));
export function codedMessage(value:unknown,status:number):CodedMessage|null {
 if(!record(value)||typeof value.code!=="string"||!Object.hasOwn(errorCodes,value.code))return null;
 const code=value.code as keyof typeof errorCodes;const definition=errorCodes[code];
 if(definition.status!==status)return null;
 const raw=value.parameters??{};if(!record(raw))return null;
 if(Object.keys(raw).length!==definition.parameters.length)return null;
 const parameters:Record<string,string|number>={};
 for(const key of definition.parameters){const v=raw[key];
  if(key==="field"&&typeof v==="string"&&fieldLabels.has(v))parameters[key]=v;
  else if(key==="max"&&typeof v==="number"&&Number.isInteger(v)&&v>=1&&v<=1000)parameters[key]=v;
  else return null;
 }
 return {code,parameters};
}
export function translateCode(value:CodedMessage,locale:string):string {
 const language=resolveLocale(locale);const catalog=language==="en"?en:tr;
 const t=createTranslator({locale:language,messages:catalog.apiErrors});
 const parameters={...value.parameters};
 if(typeof parameters.field==="string")parameters.field=catalog.apiFields[parameters.field as keyof typeof catalog.apiFields];
 return t(value.code,parameters);
}
export function problemDetails(status:number,body:unknown,locale:string):ProblemFeedback {
 const fallback=status===401?"errors.session":status===403?"errors.forbidden":status===404?"errors.notFound":status===400?"errors.validation":status===409?"errors.conflict":"errors.unexpected";
 const coded=codedMessage(body,status);const hasCode=record(body)&&Object.hasOwn(body,"code");
 const message=coded?translateCode(coded,locale):!hasCode&&record(body)&&status===409&&typeof body.title==="string"?translateProblemText(body.title,locale,fallback):translateProblemText("",locale,fallback);
 const errors:Record<string,string[]>={};
 if(record(body)&&status===400&&record(body.fieldIssues))for(const [field,issues]of Object.entries(body.fieldIssues)){
  const name=field.charAt(0).toLowerCase()+field.slice(1);if(!fields.has(name)||!Array.isArray(issues)||!issues.length)continue;
  errors[name]=issues.slice(0,3).map(issue=>{const safe=codedMessage(issue,400);return safe?translateCode(safe,locale):translateProblemText("",locale);});
 }
 else if(!hasCode&&record(body)&&status===400&&record(body.errors))for(const [field,messages]of Object.entries(body.errors)){
  const name=field.charAt(0).toLowerCase()+field.slice(1);
  if(fields.has(name)&&Array.isArray(messages)&&messages.length)errors[name]=messages.slice(0,3).map(value=>translateProblemText(typeof value==="string"?value:"",locale));
 }
 return {message,errors,code:coded?.code??null,parameters:coded?.parameters??{},severity:coded?errorCodes[coded.code].severity:status===409?"warning":"error"};
}
export function connectionError(locale:string){return translateProblemText("",locale,"errors.network");}
