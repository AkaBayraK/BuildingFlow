// AI Asistan FAZ 1: strict readers for /api/v1/assistant. Unknown tools, kinds or links are rejected, never rendered.
export const assistantPath="/api/v1/assistant";
export const assistantRoles=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"] as const;
export const assistantAdminRoles=["SUPER_ADMIN","SITE_ADMIN"] as const;
export const assistantTools=["DAILY_SUMMARY","DUES_COLLECTION","BUDGET_VARIANCE","UPCOMING_MAINTENANCE","OPEN_REQUESTS","REQUEST_BACKLOG","SUPPLIER_WATCH","SURVEY_SUMMARY","RESIDENT_BALANCE","RESIDENT_REQUESTS","RESIDENT_NOTICES"] as const;
// Resident tools only run in the mobile assistant; the web reads them to administer their ready questions.
export const residentTools=["RESIDENT_BALANCE","RESIDENT_REQUESTS","RESIDENT_NOTICES"] as const;
export const assistantCategories=["OVERVIEW","FINANCE","OPERATIONS","REQUESTS","COMMUNICATION","RESIDENT"] as const;
export const assistantFactKinds=["MONEY","COUNT","PERCENT","DATE","DAYS","SCORE"] as const;
export const announcementCategories=["GENERAL","MEETING","DUES","MAINTENANCE","URGENT","OTHER"] as const;
export const assistantNoteLimit=1000;
export const assistantMessageLimit=500;
export type AssistantTool=typeof assistantTools[number];
export type AssistantCategory=typeof assistantCategories[number];
export type AssistantFactKind=typeof assistantFactKinds[number];
export type AssistantSource="AKAAI"|"RULES";
export type AssistantQuota={dailyLimit:number;usedToday:number;remaining:number};
export type AssistantStatus={enabled:boolean;mode:AssistantSource;quota:AssistantQuota;tools:AssistantTool[];canManagePrompts:boolean;readOnly:true};
export type AssistantPrompt={id:string;code:string;category:AssistantCategory;toolKey:AssistantTool;titleTr:string;titleEn:string;textTr:string;textEn:string;sortOrder:number;isEnabled:boolean;version:number};
export type AssistantCard={tool:AssistantTool;key:string;value:string;kind:AssistantFactKind;dimension:string|null;from:string;to:string;href:string;ref:string|null;label:string|null};
export type AnnouncementDraftCategory=typeof announcementCategories[number];
export type AssistantDraft={title:string;content:string;category:AnnouncementDraftCategory;source:AssistantSource;modelFallback:boolean;masked:boolean;quota:AssistantQuota;saved:false};
export type AssistantAnswer={status:"ANSWERED"|"UNSUPPORTED";tool:AssistantTool|null;source:AssistantSource;modelFallback:boolean;answer:string;sources:AssistantCard[];quota:AssistantQuota;readOnly:true};
export type AssistantPromptForm={code:string;category:AssistantCategory;toolKey:AssistantTool;titleTr:string;titleEn:string;textTr:string;textEn:string;sortOrder:string;isEnabled:boolean};

const guid="[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}";
const hrefPattern=new RegExp(`^(?:/dashboard\\?property=${guid}|/properties/${guid}/(?:dues|expenses|maintenance|requests(?:\\?(?:status=(?:RECORDED|IN_PROGRESS|RESOLVED|REJECTED)|request=${guid}))?|surveys(?:\\?survey=${guid})?)|/suppliers(?:\\?view=alerts)?)$`,"i");
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
 if(!hrefPattern.test(href)||!href.startsWith("/suppliers")&&!href.toLowerCase().includes(propertyId.toLowerCase())||!datePattern.test(from)||!datePattern.test(to)||!/^[a-zA-Z0-9]{1,40}$/.test(text(v.key,40))||!/^[0-9.-]{1,24}$/.test(text(v.value,24)))throw new Error("Invalid assistant source");
 const dimension=v.dimension===null||v.dimension===undefined?null:text(v.dimension,40);if(dimension!==null&&!/^[A-Z0-9_-]{1,40}$/.test(dimension))throw new Error("Invalid assistant dimension");
 const ref=v.ref===null||v.ref===undefined?null:text(v.ref,4);if(ref!==null&&!/^[A-Z]\d{1,3}$/.test(ref))throw new Error("Invalid assistant alias");
 const label=v.label===null||v.label===undefined?null:text(v.label,600);
 return {tool:member(v.tool,assistantTools),key:v.key as string,value:v.value as string,kind:member(v.kind,assistantFactKinds),dimension,from,to,href,ref,label};
}
export function readAnswer(value:unknown,propertyId:string):AssistantAnswer{
 const v=object(value);if(v.readOnly!==true||typeof v.modelFallback!=="boolean")throw new Error("Invalid assistant answer");
 const status=member(v.status,["ANSWERED","UNSUPPORTED"] as const);const tool=v.tool===null?null:member(v.tool,assistantTools);
 const sources=list(v.sources).map(card=>readCard(card,propertyId));
 if(status==="ANSWERED"&&(!tool||sources.some(card=>card.tool!==tool))||status==="UNSUPPORTED"&&(tool!==null||sources.length>0))throw new Error("Invalid assistant answer");
 return {status,tool,source:member(v.source,["AKAAI","RULES"] as const),modelFallback:v.modelFallback,answer:text(v.answer,1200),sources,quota:readQuota(v.quota),readOnly:true};
}

export function readDraft(value:unknown):AssistantDraft{
 const v=object(value);if(v.saved!==false||typeof v.modelFallback!=="boolean"||typeof v.masked!=="boolean")throw new Error("Invalid assistant draft");
 const title=text(v.title,200),content=text(v.content,2000);if(!title.trim()||!content.trim())throw new Error("Invalid assistant draft");
 return {title,content,category:member(v.category,announcementCategories),source:member(v.source,["AKAAI","RULES"] as const),modelFallback:v.modelFallback,masked:v.masked,quota:readQuota(v.quota),saved:false};
}
export function draftBody(propertyId:string,locale:"tr"|"en",note:string,category:AnnouncementDraftCategory){return {propertyId,note:note.trim(),category,locale};}
export function canDraft(note:string){const value=note.trim();return value.length>0&&value.length<=assistantNoteLimit&&!/[\u0000-\u0008\u000b\u000c\u000e-\u001f\u007f]/.test(value);}
// The draft travels to the announcement editor through this tab's session storage once, then is removed; it is never saved by the assistant.
export const draftStorageKey=(propertyId:string)=>`bf.assistant.announcementDraft.${propertyId.toLowerCase()}`;
export function draftEditorHref(propertyId:string){return `/properties/${propertyId}/announcements?assistantDraft=1`;}
export function takeStoredDraft(storage:Pick<Storage,"getItem"|"removeItem">,propertyId:string):{title:string;content:string;category:AnnouncementDraftCategory}|null{
 const key=draftStorageKey(propertyId);let raw:string|null=null;try{raw=storage.getItem(key);storage.removeItem(key);}catch{return null;}
 if(!raw)return null;
 try{const v=object(JSON.parse(raw));const title=text(v.title,200).trim(),content=text(v.content,10000).trim();if(!title||!content)return null;return {title,content,category:member(v.category,announcementCategories)};}catch{return null;}
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
 if(card.tool==="REQUEST_BACKLOG"&&card.key==="requestsByStatus")return {namespace:"requests",key:`status_${card.dimension}`};
 if(card.tool==="OPEN_REQUESTS"||card.tool==="REQUEST_BACKLOG")return {namespace:"requests",key:`category_${card.dimension}`};
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

// Sesli yazışma: management roles only; speech is transcribed by the server's local STT, never by the browser vendor.
export const voiceRoles=["SUPER_ADMIN","SITE_ADMIN","MANAGER"] as const;
export const voiceRecorderTypes=["audio/webm;codecs=opus","audio/webm","audio/ogg;codecs=opus","audio/mp4"] as const;
export type AssistantVoiceStatus={enabled:boolean;reason:"PROVIDER_NOT_CONFIGURED"|"PROVIDER_UNREACHABLE"|null;maxSeconds:number;maxBytes:number};
export function readVoiceStatus(value:unknown):AssistantVoiceStatus{
 const v=object(value);if(typeof v.enabled!=="boolean")throw new Error("Invalid voice status");
 const reason=v.reason===null?null:member(v.reason,["PROVIDER_NOT_CONFIGURED","PROVIDER_UNREACHABLE"] as const);if(v.enabled&&reason!==null||!v.enabled&&reason===null)throw new Error("Invalid voice status");
 const maxSeconds=count(v.maxSeconds),maxBytes=count(v.maxBytes);if(maxSeconds<1||maxSeconds>300||maxBytes<1)throw new Error("Invalid voice status");
 return {enabled:v.enabled,reason,maxSeconds,maxBytes};
}
export function readTranscript(value:unknown):{text:string;quota:AssistantQuota}{const v=object(value);return {text:text(v.text,assistantMessageLimit),quota:readQuota(v.quota)};}
export function pickRecorderType(supported:(type:string)=>boolean){return voiceRecorderTypes.find(type=>supported(type))??null;}
export function transcribeForm(propertyId:string,audio:Blob,durationMs:number,locale:"tr"|"en"){
 const form=new FormData();form.set("audio",audio,"speech");form.set("propertyId",propertyId);form.set("durationMs",String(Math.max(1,Math.round(durationMs))));form.set("locale",locale);return form;
}
// The transcript is appended for the person to review; it is never sent on its own.
export function mergeTranscript(current:string,transcript:string){const t=transcript.trim();if(!t)return current;const base=current.trim();return (base?base+" "+t:t).slice(0,assistantMessageLimit);}
export const voicePreferenceKey="bf.assistant.voice";
export type VoicePreference={voice:boolean;readAloud:boolean};
export type MicrophoneProblem="voiceMicDenied"|"voiceMicMissing"|"voiceMicBlocked"|"voiceMicBusy"|"voiceMicInsecure"|"voicePermission";
export function microphoneProblem(name:string|undefined,secure:boolean):MicrophoneProblem{if(!secure)return "voiceMicInsecure";switch(name){case "NotAllowedError":case "PermissionDeniedError":return "voiceMicDenied";case "NotFoundError":case "DevicesNotFoundError":case "OverconstrainedError":return "voiceMicMissing";case "SecurityError":return "voiceMicBlocked";case "NotReadableError":case "AbortError":return "voiceMicBusy";default:return "voicePermission";}}
export function readVoicePreference(raw:string|null):VoicePreference{try{const v=raw?JSON.parse(raw):null;return {voice:v?.voice===true,readAloud:v?.readAloud===true};}catch{return {voice:false,readAloud:false};}}

// whisper.cpp's server reads 16 kHz mono WAV without ffmpeg, so the recording is re-encoded in the browser (Web Audio, no package).
export const transcriptionRate=16000;
export function downmix(channels:readonly Float32Array[],sourceRate:number,targetRate=transcriptionRate):Float32Array{
 const length=channels[0]?.length??0;if(!length||sourceRate<=0)return new Float32Array(0);
 const ratio=sourceRate/targetRate,out=new Float32Array(Math.max(1,Math.floor(length/ratio)));
 for(let i=0;i<out.length;i++){
  const start=Math.floor(i*ratio),end=Math.min(length,Math.max(start+1,Math.floor((i+1)*ratio)));let sum=0;
  for(let j=start;j<end;j++){let s=0;for(const c of channels)s+=c[j];sum+=s/channels.length;}
  out[i]=sum/(end-start);
 }
 return out;
}
export function encodeWav(samples:Float32Array,rate=transcriptionRate):ArrayBuffer{
 const buffer=new ArrayBuffer(44+samples.length*2),view=new DataView(buffer);
 const ascii=(offset:number,text:string)=>{for(let i=0;i<text.length;i++)view.setUint8(offset+i,text.charCodeAt(i));};
 ascii(0,"RIFF");view.setUint32(4,36+samples.length*2,true);ascii(8,"WAVE");ascii(12,"fmt ");view.setUint32(16,16,true);view.setUint16(20,1,true);view.setUint16(22,1,true);
 view.setUint32(24,rate,true);view.setUint32(28,rate*2,true);view.setUint16(32,2,true);view.setUint16(34,16,true);ascii(36,"data");view.setUint32(40,samples.length*2,true);
 for(let i=0;i<samples.length;i++){const s=Math.max(-1,Math.min(1,samples[i]));view.setInt16(44+i*2,s<0?s*0x8000:s*0x7fff,true);}
 return buffer;
}
