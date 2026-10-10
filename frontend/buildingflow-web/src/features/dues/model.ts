export type ChargeStatus="OPEN"|"OVERDUE"|"CANCELLED"|"PARTIALLY_PAID"|"PAID";
export type DuesParty={chargeId:string;personId:string;occupancyId:string;personName:string;type:"OWNER"|"TENANT"|"RESIDENT";startDate:string;endDate:string|null;isDebtor:boolean};
export type DuesCharge={id:string;propertyId:string;assessmentId:string;unitId:string;buildingId:string|null;unitNumber:string;buildingName:string|null;period:string;amount:string;balance:string;currency:"TRY";dueDate:string;description:string;status:ChargeStatus;version:number;createdAt:string;cancellationReason:string|null;cancelledAt:string|null;partiesRecorded:boolean;partiesVisible:boolean;debtorType:"UNIT"|"TENANT"|"OWNER";parties:DuesParty[];cancellationReasonCode:string|null;cancellationReasonName:string|null};
export type DuesPage={items:DuesCharge[];page:number;pageSize:number;totalCount:number;summary:{outstandingAmount:string;overdueAmount:string;cancelledAmount:string;openCount:number;overdueCount:number;cancelledCount:number;currency:"TRY"}};
export type Assessment={id:string;propertyId:string;requestId:string;period:string;amount:string;currency:"TRY";dueDate:string;description:string;unitCount:number;totalAmount:string;unitIds:string[];replayed:boolean};
export type SubmissionKey={signature:string;id:string};
export function submissionKey(previous:SubmissionKey|null,signature:string,createId:()=>string):SubmissionKey{
 return previous?.signature===signature?previous:{signature,id:createId()};
}
// No grouping separator and no rounding: protocol money is always decimal text.
export function normalizeAmount(raw:string):string|null{
 const text=raw.trim();if(!/^[0-9]{1,8}(?:[.,][0-9]{1,2})?$/.test(text))return null;
 const [whole,fraction=""]=text.replace(",",".").split(".");
 const cents=BigInt(whole)*BigInt(100)+BigInt(fraction.padEnd(2,"0"));
 if(cents<=BigInt(0)||cents>BigInt(9999999999))return null;
 return (cents/BigInt(100)).toString()+"."+(cents%BigInt(100)).toString().padStart(2,"0");
}
export function previewTotal(amount:string,count:number):string{
 const normalized=normalizeAmount(amount);if(!normalized||!Number.isSafeInteger(count)||count<1||count>500)throw new Error("Invalid assessment preview");
 const [whole,fraction]=normalized.split(".");const cents=(BigInt(whole)*BigInt(100)+BigInt(fraction))*BigInt(count);
 return (cents/BigInt(100)).toString()+"."+(cents%BigInt(100)).toString().padStart(2,"0");
}
function object(value:unknown):value is Record<string,unknown>{return typeof value==="object"&&value!==null&&!Array.isArray(value);}
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const text=(v:unknown):v is string=>typeof v==="string";
const nullableText=(v:unknown)=>v===null||text(v);
const money=(v:unknown)=>text(v)&&/^[0-9]{1,18}\.[0-9]{2}$/.test(v);
const integer=(v:unknown,min=0):v is number=>typeof v==="number"&&Number.isSafeInteger(v)&&v>=min;
const cents=(value:string)=>BigInt(value.replace(".",""));
export function isDuesCharge(value:unknown):value is DuesCharge{
 if(!object(value))return false;
 if(typeof value.partiesRecorded!=="boolean"||typeof value.partiesVisible!=="boolean"||!['UNIT','TENANT','OWNER'].includes(value.debtorType as string)||
  !text(value.period)||!Array.isArray(value.parties)||!value.parties.every(p=>isDuesParty(p,value.id as string)&&p.startDate<=(value.period as string)+'-01'&&(p.endDate===null||p.endDate>=(value.period as string)+'-01'))||new Set(value.parties.map(p=>p.occupancyId)).size!==value.parties.length||
  ((!value.partiesRecorded||!value.partiesVisible)&&value.parties.length>0)||!nullableText(value.cancellationReasonCode)||!nullableText(value.cancellationReasonName)||
  ((value.cancellationReasonCode===null)!==(value.cancellationReasonName===null))||
  (value.status!=='CANCELLED'&&value.cancellationReasonCode!==null))return false;
 const debtors=value.parties.filter(p=>p.isDebtor);
 if(value.debtorType==='UNIT'?debtors.length!==0:value.partiesVisible&&value.partiesRecorded&&(debtors.length!==1||debtors[0].type!==value.debtorType))return false;
 return ["id","propertyId","assessmentId","unitId"].every(k=>guid(value[k]))&&
  (value.buildingId===null||guid(value.buildingId))&&["unitNumber","period","dueDate","description","createdAt"].every(k=>text(value[k]))&&
  nullableText(value.buildingName)&&nullableText(value.cancellationReason)&&nullableText(value.cancelledAt)&&
  money(value.amount)&&money(value.balance)&&value.currency==="TRY"&&
  text(value.status)&&["OPEN","OVERDUE","CANCELLED","PARTIALLY_PAID","PAID"].includes(value.status)&&
  cents(value.amount as string)>BigInt(0)&&cents(value.amount as string)<=BigInt(9999999999)&&cents(value.balance as string)<=cents(value.amount as string)&&
  (value.status==="PAID"||value.status==="CANCELLED"?cents(value.balance as string)===BigInt(0):value.status==="PARTIALLY_PAID"?cents(value.balance as string)>BigInt(0)&&cents(value.balance as string)<cents(value.amount as string):value.status==="OPEN"?cents(value.balance as string)===cents(value.amount as string):cents(value.balance as string)>BigInt(0))&&integer(value.version,1);
}
function calendar(value:unknown):value is string{
 if(!text(value)||!/^\d{4}-\d{2}-\d{2}$/.test(value))return false;
 const date=new Date(value+'T00:00:00Z');return Number.isFinite(date.getTime())&&date.toISOString().slice(0,10)===value;
}
function isDuesParty(value:unknown,chargeId:string):value is DuesParty{
 return object(value)&&value.chargeId===chargeId&&guid(value.personId)&&guid(value.occupancyId)&&text(value.personName)&&value.personName.trim().length>0&&value.personName.length<=201&&
  ['OWNER','TENANT','RESIDENT'].includes(value.type as string)&&calendar(value.startDate)&&(value.endDate===null||(calendar(value.endDate)&&value.endDate>=value.startDate))&&
  typeof value.isDebtor==='boolean'&&(!value.isDebtor||value.type!=='RESIDENT');
}
export type DuesPersonChoice={id:string;propertyId:string;name:string};
export function readDuesPersonChoices(value:unknown,propertyId:string):{items:DuesPersonChoice[];page:number;pageSize:number;totalCount:number}{
 if(!object(value)||!Array.isArray(value.items)||!integer(value.page,1)||!integer(value.pageSize,1)||value.pageSize>100||!integer(value.totalCount)||value.items.length>value.pageSize||value.items.length>value.totalCount||
  !value.items.every(p=>object(p)&&guid(p.id)&&p.propertyId===propertyId&&text(p.name)&&p.name.trim().length>0&&p.name.length<=201)||
  new Set(value.items.map(p=>p.id)).size!==value.items.length)throw new Error('Invalid dues people');
 return value as {items:DuesPersonChoice[];page:number;pageSize:number;totalCount:number};
}
export type CancellationReason={id:string;code:string;name:string;requiresExplanation:boolean};
export function readCancellationReasons(value:unknown):CancellationReason[]{
 if(!Array.isArray(value)||!value.every(r=>object(r)&&guid(r.id)&&text(r.code)&&/^[A-Z_]{1,50}$/.test(r.code)&&text(r.name)&&r.name.trim().length>0&&r.name.length<=100&&typeof r.requiresExplanation==='boolean')||
  new Set(value.map(r=>r.id)).size!==value.length||new Set(value.map(r=>r.code)).size!==value.length)throw new Error('Invalid cancellation reasons');
 return value as CancellationReason[];
}
export type MoveOutPreview={occupancyId:string;version:number;endDate:string;eligibleCount:number;totalAmount:string;skippedPaymentCount:number;items:{id:string;version:number;period:string;amount:string;dueDate:string}[];approvalAvailable:boolean};
export function readMoveOutPreview(value:unknown,occupancyId:string,version:number,endDate:string):MoveOutPreview{
 if(!object(value)||value.occupancyId!==occupancyId||value.version!==version||value.endDate!==endDate||!calendar(value.endDate)||!integer(value.eligibleCount)||!integer(value.skippedPaymentCount)||!money(value.totalAmount)||typeof value.approvalAvailable!=='boolean'||!Array.isArray(value.items)||
  (value.approvalAvailable?value.eligibleCount>500||value.items.length!==value.eligibleCount:value.eligibleCount<=500||value.items.length!==0)||
  !value.items.every(p=>object(p)&&guid(p.id)&&integer(p.version,1)&&text(p.period)&&/^\d{4}-(?:0[1-9]|1[0-2])$/.test(p.period)&&p.period+'-01'>endDate&&money(p.amount)&&cents(p.amount as string)>BigInt(0)&&cents(p.amount as string)<=BigInt(9999999999)&&calendar(p.dueDate)&&p.dueDate>=p.period+'-01')||
  new Set(value.items.map(p=>p.id)).size!==value.items.length)throw new Error('Invalid move-out preview');
 if(value.approvalAvailable&&value.items.reduce((sum,p)=>sum+cents(p.amount),BigInt(0))!==cents(value.totalAmount as string))throw new Error('Invalid move-out total');
 return value as MoveOutPreview;
}
export function readTenantDebtResult(value:unknown):{cancelledCount:number;pendingCount:number;skippedPaymentCount:number}{
 if(!object(value)||!object(value.tenantDebtResult))throw new Error('Invalid move-out result');
 const result=value.tenantDebtResult;
 if(!['cancelledCount','pendingCount','skippedPaymentCount'].every(k=>integer(result[k])))throw new Error('Invalid move-out result');
 return result as {cancelledCount:number;pendingCount:number;skippedPaymentCount:number};
}
export function readDuesPage(value:unknown,propertyId:string):DuesPage{
 if(!object(value)||!Array.isArray(value.items)||!value.items.every(v=>isDuesCharge(v)&&v.propertyId===propertyId)||
  !integer(value.page,1)||!integer(value.pageSize,1)||!integer(value.totalCount)||!object(value.summary))throw new Error("Invalid dues response");
 const summary=value.summary;
 if(
  !["outstandingAmount","overdueAmount","cancelledAmount"].every(k=>money(summary[k]))||
  !["openCount","overdueCount","cancelledCount"].every(k=>integer(summary[k]))||summary.currency!=="TRY")
  throw new Error("Invalid dues response");
 return value as DuesPage;
}
export function readCharge(value:unknown,propertyId:string):DuesCharge{
 if(!isDuesCharge(value)||value.propertyId!==propertyId)throw new Error("Invalid charge response");return value;
}
export function readAssessment(value:unknown,propertyId:string):Assessment{
 if(!object(value)||!guid(value.id)||value.propertyId!==propertyId||!guid(value.requestId)||
  !["period","dueDate","description"].every(k=>text(value[k]))||!money(value.amount)||!money(value.totalAmount)||
  value.currency!=="TRY"||!integer(value.unitCount,1)||!Array.isArray(value.unitIds)||!value.unitIds.every(guid)||
  value.unitIds.length!==value.unitCount||typeof value.replayed!=="boolean")throw new Error("Invalid assessment response");
 return value as Assessment;
}
export type UnitChoicesPage={items:{id:string;buildingId:string|null;number:string;buildingName:string|null;isEnabled:boolean}[];page:number;pageSize:number;totalCount:number};
export function readUnitChoices(value:unknown):UnitChoicesPage{
 if(!object(value)||!Array.isArray(value.items)||!integer(value.page,1)||!integer(value.pageSize,1)||!integer(value.totalCount)||
  !value.items.every(v=>object(v)&&guid(v.id)&&(v.buildingId===null||guid(v.buildingId))&&text(v.number)&&nullableText(v.buildingName)&&typeof v.isEnabled==="boolean"))throw new Error("Invalid unit choices response");
 return value as UnitChoicesPage;
}

// Only the explicitly reviewed list may be approved; a date/version change invalidates consent.
export function moveOutApprovals(preview:MoveOutPreview|null,occupancyId:string,version:number,endDate:string,approved:boolean){
 if(!approved)return [];
 if(!preview||preview.occupancyId!==occupancyId||preview.version!==version||preview.endDate!==endDate||!preview.approvalAvailable)throw new Error('Refresh move-out preview');
 return preview.items.map(item=>({chargeId:item.id,version:item.version}));
}
