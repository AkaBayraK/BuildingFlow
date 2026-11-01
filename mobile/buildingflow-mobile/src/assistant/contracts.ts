// AI Asistan FAZ 3 (MOBİL): strict readers for /api/v1/mobile/assistant. The assistant is off unless the server says otherwise.
export const assistantRoles:readonly string[]=['OWNER','TENANT'];
export const assistantTools=['RESIDENT_BALANCE','RESIDENT_REQUESTS','RESIDENT_NOTICES'] as const;
export const assistantKinds=['MONEY','COUNT','PERCENT','DATE','DAYS','SCORE'] as const;
export const assistantMessageLimit=500;
export type AssistantPath='status'|'prompts'|'chat';
export type AssistantTool=typeof assistantTools[number];
export type AssistantGate='hidden'|'consent'|'ready';
export type AssistantQuota={dailyLimit:number;usedToday:number;remaining:number};
export type AssistantStatus={mode:'AKAAI'|'RULES';quota:AssistantQuota};
export type AssistantPrompt={code:string;toolKey:AssistantTool;titleTr:string;textTr:string;sortOrder:number};
export type AssistantCard={tool:AssistantTool;key:string;value:string;kind:typeof assistantKinds[number];dimension:string|null;from:string;to:string;href:'/finance'|'/requests'|'/announcements';label:string|null};
export type AssistantAnswer={status:'ANSWERED'|'UNSUPPORTED';tool:AssistantTool|null;source:'AKAAI'|'RULES';modelFallback:boolean;answer:string;sources:AssistantCard[];quota:AssistantQuota};
type Row=Record<string,unknown>;
const hrefs=['/finance','/requests','/announcements'] as const;
function object(v:unknown):Row{if(!v||typeof v!=='object'||Array.isArray(v))throw Error('Asistan yanıtı geçersiz.');return v as Row;}
function text(v:unknown,max:number):string{if(typeof v!=='string'||v.length>max)throw Error('Asistan metni geçersiz.');return v;}
function count(v:unknown):number{if(typeof v!=='number'||!Number.isSafeInteger(v)||v<0)throw Error('Asistan sayısı geçersiz.');return v;}
function member<T extends string>(v:unknown,values:readonly T[]):T{if(typeof v!=='string'||!values.includes(v as T))throw Error('Asistan kodu geçersiz.');return v as T;}
const date=/^\d{4}-\d{2}-\d{2}$/;

// 403 with the disabled code (or any failure) hides the assistant; the consent code offers the existing consent flow.
export function assistantGate(status:number,body:unknown):AssistantGate{
 if(status===200)return 'ready';
 const code=body&&typeof body==='object'?(body as Row).code:null;
 return status===403&&code==='BF_CONSENT_REQUIRED'?'consent':'hidden';
}
export function readQuota(v:unknown):AssistantQuota{const q=object(v);const value={dailyLimit:count(q.dailyLimit),usedToday:count(q.usedToday),remaining:count(q.remaining)};if(value.dailyLimit<1||value.dailyLimit>100)throw Error('Asistan kotası geçersiz.');return value;}
export function readStatus(v:unknown):AssistantStatus{const s=object(v);if(s.enabled!==true||s.readOnly!==true)throw Error('Asistan durumu geçersiz.');return {mode:member(s.mode,['AKAAI','RULES'] as const),quota:readQuota(s.quota)};}
export function readPrompts(v:unknown):AssistantPrompt[]{
 if(!Array.isArray(v)||v.length>50)throw Error('Hazır sorular geçersiz.');
 return v.map(item=>{const p=object(item);const code=text(p.code,40);if(!/^[A-Z][A-Z0-9_]{1,39}$/.test(code)||p.isEnabled!==true)throw Error('Hazır soru geçersiz.');
  return {code,toolKey:member(p.toolKey,assistantTools),titleTr:text(p.titleTr,120),textTr:text(p.textTr,500),sortOrder:count(p.sortOrder)};});
}
export function readCard(v:unknown):AssistantCard{
 const c=object(v);const from=text(c.from,10),to=text(c.to,10),value=text(c.value,24),key=text(c.key,40);
 if(!date.test(from)||!date.test(to)||!/^[0-9.-]{1,24}$/.test(value)||!/^[a-zA-Z0-9]{1,40}$/.test(key))throw Error('Kaynak kartı geçersiz.');
 const dimension=c.dimension===null||c.dimension===undefined?null:text(c.dimension,40);if(dimension!==null&&!/^[A-Z0-9_-]{1,40}$/.test(dimension))throw Error('Kaynak kartı geçersiz.');
 return {tool:member(c.tool,assistantTools),key,value,kind:member(c.kind,assistantKinds),dimension,from,to,href:member(c.href,hrefs),label:c.label===null||c.label===undefined?null:text(c.label,600)};
}
export function readAnswer(v:unknown):AssistantAnswer{
 const a=object(v);if(a.readOnly!==true||typeof a.modelFallback!=='boolean')throw Error('Asistan cevabı geçersiz.');
 const status=member(a.status,['ANSWERED','UNSUPPORTED'] as const),tool=a.tool===null?null:member(a.tool,assistantTools);
 if(!Array.isArray(a.sources)||a.sources.length>40)throw Error('Asistan cevabı geçersiz.');const sources=a.sources.map(readCard);
 if(status==='ANSWERED'&&(!tool||sources.some(s=>s.tool!==tool))||status==='UNSUPPORTED'&&(tool!==null||sources.length>0))throw Error('Asistan cevabı tutarsız.');
 return {status,tool,source:member(a.source,['AKAAI','RULES'] as const),modelFallback:a.modelFallback,answer:text(a.answer,1200),sources,quota:readQuota(a.quota)};
}
export function canAsk(message:string){const v=message.trim();return v.length>0&&v.length<=assistantMessageLimit&&!/[\u0000-\u0008\u000b\u000c\u000e-\u001f\u007f]/.test(v);}
export function chatBody(context:{propertyId:string;unitId:string|null;role:string},input:{promptCode:string}|{message:string}){
 if(!context.unitId||!assistantRoles.includes(context.role))throw Error('Daire bağlamı gerekli.');
 const base={propertyId:context.propertyId,unitId:context.unitId,role:context.role,locale:'tr'};
 return 'promptCode' in input?{...base,promptCode:input.promptCode}:{...base,message:input.message.trim()};
}
// Turkish labels for the card keys the server sends; an unknown key is shown neutrally rather than guessed.
export const factLabels:Record<string,string>={myBalance:'Toplam borç',myOverdue:'Geciken tutar',myPaidTotal:'Ödenen toplam',myPenalty:'Gecikme tazminatı',myNextDueDate:'Sonraki son ödeme tarihi',myNextDueAmount:'Sonraki ödeme tutarı',myPayment:'Ödeme',
 myRequestsByStatus:'Talep durumu',myRequestUpdated:'Son güncellemeden bu yana',announcementCount:'Duyuru',unreadAnnouncements:'Okunmamış duyuru',announcementDate:'Duyuru tarihi',documentDate:'Belge tarihi'};
export const statusLabels:Record<string,string>={RECORDED:'Kaydedildi',IN_PROGRESS:'İşlemde',RESOLVED:'Çözüldü',REJECTED:'Reddedildi'};
export function cardValue(card:AssistantCard){
 if(card.kind==='MONEY')return Number(card.value).toLocaleString('tr-TR',{minimumFractionDigits:2,maximumFractionDigits:2})+' TL';
 if(card.kind==='DATE')return new Date(card.value+'T00:00:00').toLocaleDateString('tr-TR',{day:'numeric',month:'long',year:'numeric'});
 if(card.kind==='DAYS')return card.value+' gün';
 if(card.kind==='PERCENT')return '%'+Number(card.value).toLocaleString('tr-TR');
 return Number(card.value).toLocaleString('tr-TR');
}
