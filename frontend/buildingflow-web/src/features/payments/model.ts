export type PaymentMethod="CASH"|"BANK_TRANSFER";
export type PaymentRecord={id:string;propertyId:string;unitId:string;buildingId:string|null;unitNumber:string;buildingName:string|null;requestId:string;amount:string;currency:"TRY";paidDate:string;method:PaymentMethod;reference:string|null;description:string;status:"RECORDED"|"CANCELLED";version:number;createdAt:string;cancellationReason:string|null;cancelledAt:string|null;allocations:{id:string;chargeId:string;period:string;amount:string}[];replayed:boolean};
export type PaymentPage={items:PaymentRecord[];page:number;pageSize:number;totalCount:number;summary:{recordedAmount:string;cancelledAmount:string;recordedCount:number;cancelledCount:number;currency:"TRY"}};
const object=(v:unknown):v is Record<string,unknown>=>v!==null&&typeof v==="object"&&!Array.isArray(v);
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const text=(v:unknown):v is string=>typeof v==="string";
const nullableText=(v:unknown)=>v===null||text(v);
const integer=(v:unknown,min=0)=>typeof v==="number"&&Number.isSafeInteger(v)&&v>=min;
const money=(v:unknown):v is string=>text(v)&&/^(?:0|[1-9][0-9]{0,17})\.[0-9]{2}$/.test(v);
const date=(v:unknown):v is string=>text(v)&&/^[2-9][0-9]{3}-[0-9]{2}-[0-9]{2}$/.test(v)&&Number.isFinite(Date.parse(v))&&new Date(v).toISOString().slice(0,10)===v;
const timestamp=(v:unknown)=>text(v)&&Number.isFinite(Date.parse(v));
export function moneyCents(value:string):bigint{if(!money(value))throw new Error("Invalid money");const [whole,fraction]=value.split(".");return BigInt(whole)*BigInt(100)+BigInt(fraction);}
export function allocationTotal(values:string[]):string|null{
 if(values.length<1||values.length>100)return null;let total=BigInt(0);
 for(const value of values){if(!money(value))return null;const cents=moneyCents(value);if(cents<=BigInt(0)||cents>BigInt(9999999999))return null;total+=cents;}
 return total>BigInt(9999999999)?null:`${total/BigInt(100)}.${(total%BigInt(100)).toString().padStart(2,"0")}`;
}
export function readPayment(value:unknown,propertyId:string):PaymentRecord{
 if(!object(value)||!["id","propertyId","unitId","requestId"].every(k=>guid(value[k]))||value.propertyId!==propertyId||
  !(value.buildingId===null||guid(value.buildingId))||!text(value.unitNumber)||!nullableText(value.buildingName)||
  !money(value.amount)||moneyCents(value.amount)<=BigInt(0)||moneyCents(value.amount)>BigInt(9999999999)||value.currency!=="TRY"||!date(value.paidDate)||
  !text(value.method)||!["CASH","BANK_TRANSFER"].includes(value.method)||!nullableText(value.reference)||(value.method==="BANK_TRANSFER"&&(!text(value.reference)||!value.reference.trim()))||
  !text(value.description)||!value.description.trim()||!integer(value.version,1)||!timestamp(value.createdAt)||!nullableText(value.cancellationReason)||!nullableText(value.cancelledAt)||
  !text(value.status)||!["RECORDED","CANCELLED"].includes(value.status)||typeof value.replayed!=="boolean"||!Array.isArray(value.allocations)||value.allocations.length<1||value.allocations.length>100)
  throw new Error("Invalid payment response");
 if(value.status==="CANCELLED"?(!text(value.cancellationReason)||!value.cancellationReason.trim()||!timestamp(value.cancelledAt)):(value.cancellationReason!==null||value.cancelledAt!==null))throw new Error("Invalid cancellation history");
 if(!value.allocations.every(part=>object(part)&&guid(part.id)&&guid(part.chargeId)&&text(part.period)&&/^[2-9][0-9]{3}-(?:0[1-9]|1[0-2])$/.test(part.period)&&money(part.amount)&&moneyCents(part.amount)>BigInt(0)))throw new Error("Invalid allocations");
 const parts=value.allocations as PaymentRecord["allocations"];
 if(new Set(parts.map(part=>part.id)).size!==parts.length||new Set(parts.map(part=>part.chargeId)).size!==parts.length||allocationTotal(parts.map(part=>part.amount))!==value.amount)throw new Error("Invalid allocation total");
 return value as PaymentRecord;
}
export function readPaymentPage(value:unknown,propertyId:string):PaymentPage{
 if(!object(value)||!Array.isArray(value.items)||!integer(value.page,1)||!integer(value.pageSize,1)||Number(value.pageSize)>100||!integer(value.totalCount)||value.items.length>Number(value.pageSize)||value.items.length>Number(value.totalCount)||!object(value.summary))throw new Error("Invalid payment page");
 value.items.forEach(item=>readPayment(item,propertyId));
 if(new Set(value.items.map(item=>(item as PaymentRecord).id)).size!==value.items.length)throw new Error("Duplicate payment rows");
 const summary=value.summary;
 if(!money(summary.recordedAmount)||!money(summary.cancelledAmount)||!integer(summary.recordedCount)||!integer(summary.cancelledCount)||Number(summary.recordedCount)+Number(summary.cancelledCount)!==value.totalCount||summary.currency!=="TRY")throw new Error("Invalid payment summary");
 return value as PaymentPage;
}
