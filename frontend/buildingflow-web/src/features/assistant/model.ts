// AI Asistan FAZ 1: strict readers for /api/v1/assistant. Unknown tools, kinds or links are rejected, never rendered.
export const assistantPath="/api/v1/assistant";
export const assistantRoles=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"] as const;
export const assistantAdminRoles=["SUPER_ADMIN","SITE_ADMIN"] as const;
export const assistantTools=["DAILY_SUMMARY","DUES_COLLECTION","BUDGET_VARIANCE","UPCOMING_MAINTENANCE","OPEN_REQUESTS"] as const;
export const assistantCategories=["OVERVIEW","FINANCE","OPERATIONS","REQUESTS"] as const;
export const assistantFactKinds=["MONEY","COUNT","PERCENT","DATE","DAYS"] as const;
export const assistantMessageLimit=500;
export type AssistantTool=typeof assistantTools[number];
export type AssistantCategory=typeof assistantCategories[number];
export type AssistantFactKind=typeof assistantFactKinds[number];
export type AssistantSource="AKAAI"|"RULES";
export type AssistantQuota={dailyLimit:number;usedToday:number;remaining:number};
export type AssistantStatus={enabled:boolean;mode:AssistantSource;quota:AssistantQuota;tools:AssistantTool[];canManagePrompts:boolean;readOnly:true};
export type AssistantPrompt={id:string;code:string;category:AssistantCategory;toolKey:AssistantTool;titleTr:string;titleEn:string;textTr:string;textEn:string;sortOrder:number;isEnabled:boolean;version:number};
export type AssistantCard={tool:AssistantTool;key:string;value:string;kind:AssistantFactKind;dimension:string|null;from:string;to:string;href:string};
export type AssistantAnswer={status:"ANSWERED"|"UNSUPPORTED";tool:AssistantTool|null;source:AssistantSource;modelFallback:boolean;answer:string;sources:AssistantCard[];quota:AssistantQuota;readOnly:true};
export type AssistantPromptForm={code:string;category:AssistantCategory;toolKey:AssistantTool;titleTr:string;titleEn:string;textTr:string;textEn:string;sortOrder:string;isEnabled:boolean};

const guid="[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}";
const hrefPattern=new RegExp(`^(?:/dashboard\\?property=${guid}|/properties/${guid}/(?:dues|expenses|maintenance|requests))$`,"i");
const datePattern=/^\d{4}-\d{2}-\d{2}$/;
const codePattern=/^[A-Z][A-Z0-9_]{1,39}$/;
type Row=Record<string,unknown>;
function object(value:unknown):Row{if(!value||typeof value!=="object"||Array.isArray(value))throw new Error("Invalid assistant response");return value as Row;}
function text(value:unknown,max=2000):string{if(typeof value!=="string"||value.length>max)throw new Error("Invalid assistant text");return value;}
function count(value:unknown):number{if(typeof value!=="number"||!Number.isInteger(value)||value<0)throw new Error("Invalid assistant number");return value;}
function member<T extends string>(value:unknown,values:readonly T[]):T{if(typeof value!=="string"||!values.includes(value as T))throw new Error("Invalid assistant code");return value as T;}
function list(value:unknown):unknown[]{if(!Array.isArray(value))throw new Error("Invalid assistant list");return value;}

export function readQuota(value:unknown):AssistantQuota{const v=object(value);const quota={dailyLimit:count(v.dailyLimit),usedToday:count(v.usedToday),remaining:count(v.remaining)};if(quota.dailyLimit<1)throw new Error("Invalid quota");return quota;}
export function readStatus(value:unknown):AssistantStatus{
 const v=object(value);if(v.readOnly!==true||typeof v.enabled!=="boolean"||typeof v.canManagePrompts!=="boolean")throw new Error("Invalid assistant status");
 return {enabled:v.enabled,mode:member(v.mode,["AKAAI","RULES"] as const),quota:readQuota(v.quota),tools:list(v.tools).map(t=>member(t,assistantTools)),canManagePrompts:v.canManagePrompts,readOnly:true};
}
export function readPrompt(value:unknown):AssistantPrompt{
 const v=object(value);if(typeof v.id!=="string"||!new RegExp(`^${guid}$`,"i").test(v.id)||typeof v.isEnabled!=="boolean"||!codePattern.test(text(v.code,40)))throw new Error("Invalid assistant prompt");
 return {id:v.id,code:v.code as string,category:member(v.category,assistantCategories),toolKey:member(v.toolKey,assistantTools),titleTr:text(v.titleTr,120),titleEn:text(v.titleEn,120),textTr:text(v.textTr,500),textEn:text(v.textEn,500),sortOrder:count(v.sortOrder),isEnabled:v.isEnabled,version:count(v.version)};
}
export function readPrompts(value:unknown):AssistantPrompt[]{return list(value).map(readPrompt);}
export function readCard(value:unknown,propertyId:string):AssistantCard{
 const v=object(value);const href=text(v.href,200);const from=text(v.from,10),to=text(v.to,10);
 if(!hrefPattern.test(href)||!href.toLowerCase().includes(propertyId.toLowerCase())||!datePattern.test(from)||!datePattern.test(to)||!/^[a-zA-Z0-9]{1,40}$/.test(text(v.key,40))||!/^[0-9.-]{1,24}$/.test(text(v.value,24)))throw new Error("Invalid assistant source");
 const dimension=v.dimension===null||v.dimension===undefined?null:text(v.dimension,40);if(dimension!==null&&!/^[A-Z0-9_-]{1,40}$/.test(dimension))throw new Error("Invalid assistant dimension");
 return {tool:member(v.tool,assistantTools),key:v.key as string,value:v.value as string,kind:member(v.kind,assistantFactKinds),dimension,from,to,href};
}
export function readAnswer(value:unknown,propertyId:string):AssistantAnswer{
 const v=object(value);if(v.readOnly!==true||typeof v.modelFallback!=="boolean")throw new Error("Invalid assistant answer");
 const status=member(v.status,["ANSWERED","UNSUPPORTED"] as const);const tool=v.tool===null?null:member(v.tool,assistantTools);
 const sources=list(v.sources).map(card=>readCard(card,propertyId));
 if(status==="ANSWERED"&&(!tool||sources.some(card=>card.tool!==tool))||status==="UNSUPPORTED"&&(tool!==null||sources.length>0))throw new Error("Invalid assistant answer");
 return {status,tool,source:member(v.source,["AKAAI","RULES"] as const),modelFallback:v.modelFallback,answer:text(v.answer,1200),sources,quota:readQuota(v.quota),readOnly:true};
}

export function chatBody(propertyId:string,locale:"tr"|"en",input:{promptCode:string}|{message:string}){
 return "promptCode" in input?{propertyId,promptCode:input.promptCode,locale}:{propertyId,message:input.message.trim(),locale};
}
export function canAsk(message:string){const value=message.trim();return value.length>0&&value.length<=assistantMessageLimit&&!/[\u0000-\u0008\u000b\u000c\u000e-\u001f\u007f]/.test(value);}
export function promptTitle(prompt:AssistantPrompt,locale:"tr"|"en"){return locale==="en"?prompt.titleEn:prompt.titleTr;}
export function promptText(prompt:AssistantPrompt,locale:"tr"|"en"){return locale==="en"?prompt.textEn:prompt.textTr;}
// Category codes are translated with the owning module's existing dictionary; periods (YYYY or YYYY-MM) stay as they are.
export function dimensionKey(card:AssistantCard):{namespace:"requests"|"expenses"|"maintenance";key:string}|null{
 if(!card.dimension||/^\d{4}(?:-\d{2})?$/.test(card.dimension))return null;
 if(card.tool==="OPEN_REQUESTS")return {namespace:"requests",key:`category_${card.dimension}`};
 if(card.tool==="BUDGET_VARIANCE")return {namespace:"expenses",key:card.dimension};
 if(card.tool==="UPCOMING_MAINTENANCE")return {namespace:"maintenance",key:`type_${card.dimension}`};
 return null;
}
export function emptyPromptForm(sortOrder=500):AssistantPromptForm{return {code:"",category:"OVERVIEW",toolKey:"DAILY_SUMMARY",titleTr:"",titleEn:"",textTr:"",textEn:"",sortOrder:String(sortOrder),isEnabled:true};}
export function promptForm(prompt:AssistantPrompt):AssistantPromptForm{return {code:prompt.code,category:prompt.category,toolKey:prompt.toolKey,titleTr:prompt.titleTr,titleEn:prompt.titleEn,textTr:prompt.textTr,textEn:prompt.textEn,sortOrder:String(prompt.sortOrder),isEnabled:prompt.isEnabled};}
export function promptBody(form:AssistantPromptForm,version:number){
 const sortOrder=/^\d{1,3}$/.test(form.sortOrder.trim())?Number(form.sortOrder.trim()):-1;
 return {code:form.code.trim().toUpperCase(),category:form.category,toolKey:form.toolKey,titleTr:form.titleTr.trim(),titleEn:form.titleEn.trim(),textTr:form.textTr.trim(),textEn:form.textEn.trim(),sortOrder,isEnabled:form.isEnabled,version};
}
export function promptFormValid(form:AssistantPromptForm){
 const body=promptBody(form,0);
 return codePattern.test(body.code)&&body.sortOrder>=0&&body.sortOrder<=999&&[body.titleTr,body.titleEn].every(v=>v.length>0&&v.length<=120)&&[body.textTr,body.textEn].every(v=>v.length>0&&v.length<=500);
}
