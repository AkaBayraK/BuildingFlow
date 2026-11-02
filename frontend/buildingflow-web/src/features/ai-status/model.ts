// AI durumu sözleşmesi: GET /api/v1/system/ai-status. Açıklamalar kod olarak gelir; metni TR/EN sözlüğü üretir.
export const aiComponentTypes=["CONFIGURATION","SERVICE","MODEL","PROMPT"] as const;
export const aiStatuses=["UP","DEGRADED","DOWN","UNKNOWN","DISABLED"] as const;
export const aiComponentIds=["assistant-configuration","akaai-model","prompt-templates","resident-assistant","qa-center-ai"] as const;
export const aiDetails=["CONFIGURED","RULES_MODE","NOT_CONFIGURED","NOT_CHECKED","MODEL_READY","DEFAULT_MODEL_READY","MODEL_MISSING","DEFAULT_MODEL_MISSING","MODELS_UNAVAILABLE","MODELS_UNREADABLE","NOT_READY","TOKEN_FAILED","REJECTED","UNEXPECTED_RESPONSE","UNREACHABLE","INVALID_ADDRESS","PROMPTS_READY","NO_PROMPTS","PROMPTS_UNREADABLE","ENABLED","CONSENT_GATE"] as const;
export type AiComponentType=typeof aiComponentTypes[number];
export type AiStatusValue=typeof aiStatuses[number];
export type AiComponentId=typeof aiComponentIds[number];
export type AiDetail=typeof aiDetails[number]|"UNKNOWN";
export type AiStatusComponent={id:AiComponentId;type:AiComponentType;status:AiStatusValue;responseTimeMs:number|null;checkedAt:string;detail:AiDetail;count:number|null};
export type AiStatus={overallStatus:AiStatusValue;mode:"AKAAI"|"RULES";model:string|null;modelProvider:string|null;endpoint:string;locality:"LOCAL"|"EXTERNAL"|null;consoleUrl:string|null;activePromptCount:number;managementDailyLimit:number;residentEnabled:boolean;residentDailyLimit:number;qaCenterAiEnabled:boolean;totalComponents:number;upComponents:number;lastCheckedAt:string;components:AiStatusComponent[]};
export const aiStatusPath="/api/v1/system/ai-status";
export const aiStatusHref="/system-settings/ai-status";
// Backend CanManageHierarchy ile aynı roller; görünürlük yetki kanıtı değildir.
export const aiStatusRoles=["SUPER_ADMIN","SITE_ADMIN","MANAGER"] as const;

const text=(value:unknown)=>typeof value==="string"?value:"";
const optional=(value:unknown)=>typeof value==="string"&&value.trim()?value:null;
const count=(value:unknown)=>typeof value==="number"&&Number.isFinite(value)&&value>=0?Math.floor(value):0;
const oneOf=<T extends string>(values:readonly T[],value:unknown,fallback:T):T=>values.includes(value as T)?value as T:fallback;

// Yalnız http(s), kimlik bilgisi ve sorgu içermeyen yönetim bağlantısı açılır.
export function safeConsoleUrl(value:unknown):string|null{
 if(typeof value!=="string")return null;
 try{const url=new URL(value);return (url.protocol==="https:"||url.protocol==="http:")&&!url.username&&!url.password&&!url.search&&!url.hash?url.href:null;}catch{return null;}
}

export function parseAiStatus(value:unknown):AiStatus{
 const body=(value&&typeof value==="object"?value:{}) as Record<string,unknown>;
 const components=(Array.isArray(body.components)?body.components:[]).flatMap(item=>{
  if(!item||typeof item!=="object")return [];
  const row=item as Record<string,unknown>;
  if(!aiComponentIds.includes(row.id as AiComponentId))return [];
  return [{id:row.id as AiComponentId,type:oneOf(aiComponentTypes,row.type,"SERVICE"),status:oneOf(aiStatuses,row.status,"UNKNOWN"),responseTimeMs:typeof row.responseTimeMs==="number"?row.responseTimeMs:null,checkedAt:text(row.checkedAt),detail:oneOf<AiDetail>(aiDetails,row.detail,"UNKNOWN"),count:typeof row.count==="number"?count(row.count):null}];
 });
 return {overallStatus:oneOf(aiStatuses,body.overallStatus,"UNKNOWN"),mode:body.mode==="AKAAI"?"AKAAI":"RULES",model:optional(body.model),modelProvider:optional(body.modelProvider),endpoint:text(body.endpoint)||"-",
  locality:body.locality==="LOCAL"||body.locality==="EXTERNAL"?body.locality:null,consoleUrl:safeConsoleUrl(body.consoleUrl),activePromptCount:count(body.activePromptCount),
  managementDailyLimit:count(body.managementDailyLimit),residentEnabled:body.residentEnabled===true,residentDailyLimit:count(body.residentDailyLimit),qaCenterAiEnabled:body.qaCenterAiEnabled===true,
  totalComponents:components.length,upComponents:components.filter(c=>c.status==="UP").length,lastCheckedAt:text(body.lastCheckedAt),components};
}

export const filterAiComponents=(components:readonly AiStatusComponent[],type:"ALL"|AiComponentType)=>components.filter(c=>type==="ALL"||c.type===type);
