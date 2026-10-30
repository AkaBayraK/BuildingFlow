export const kinds=["COMPANY","INDIVIDUAL"] as const;
export const statuses=["DRAFT","ACTIVE","INACTIVE","BLOCKED"] as const;
export type SupplierKind=typeof kinds[number];
export type SupplierStatus=typeof statuses[number];
export type Supplier={id:string;tenantId:string;name:string;kind:SupplierKind;description:string|null;status:SupplierStatus;statusReason:string|null;isEnabled:boolean;version:number;createdAt:string;modifiedAt:string};
export type SupplierPage={items:Supplier[];page:number;pageSize:number;totalCount:number};
export type SupplierForm={name:string;kind:SupplierKind;description:string;status:SupplierStatus;statusReason:string};
const object=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
export const supplierId=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v)&&v!=="00000000-0000-0000-0000-000000000000";
const text=(v:unknown,max:number)=>typeof v==="string"&&!!v.trim()&&v.length<=max&&!/[\u0000-\u001f\u007f]/.test(v);
const optional=(v:unknown,max:number)=>v===null||text(v,max);
const integer=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
const stamp=(v:unknown)=>typeof v==="string"&&Number.isFinite(Date.parse(v));
export function readSupplier(v:unknown,tenant:string):Supplier{
 if(!object(v)||!supplierId(v.id)||v.tenantId!==tenant||!text(v.name,200)||!kinds.includes(v.kind as SupplierKind)||!statuses.includes(v.status as SupplierStatus)||!optional(v.description,2000)||!optional(v.statusReason,1000)||!integer(v.version,1)||!stamp(v.createdAt)||!stamp(v.modifiedAt)||v.isEnabled!==["DRAFT","ACTIVE"].includes(String(v.status))||["INACTIVE","BLOCKED"].includes(String(v.status))&&v.statusReason===null)throw new Error("Invalid supplier");
 return v as Supplier;
}
export function readSupplierPage(v:unknown,tenant:string):SupplierPage{
 if(!object(v)||!integer(v.page,1)||!integer(v.pageSize,1)||Number(v.pageSize)>100||!integer(v.totalCount)||!Array.isArray(v.items)||v.items.length>Number(v.pageSize)||v.items.length>Number(v.totalCount))throw new Error("Invalid supplier page");
 const items=v.items.map(x=>readSupplier(x,tenant));if(new Set(items.map(x=>x.id)).size!==items.length)throw new Error("Duplicate supplier");return {...v,items} as SupplierPage;
}
export function readSupplierMutation(v:unknown,tenant:string){
 if(!object(v)||typeof v.replayed!=="boolean"||!integer(v.appliedVersion,1))throw new Error("Invalid supplier mutation");
 const record=readSupplier(v.record,tenant);if(Number(v.appliedVersion)>record.version||!v.replayed&&v.appliedVersion!==record.version)throw new Error("Invalid supplier version");return {record,replayed:v.replayed,appliedVersion:Number(v.appliedVersion)};
}
export function supplierForm(v?:Supplier):SupplierForm{return {name:v?.name??"",kind:v?.kind??"COMPANY",description:v?.description??"",status:v?.status??"DRAFT",statusReason:v?.statusReason??""};}
export function supplierBody(form:SupplierForm,version:number){const clean=(v:string)=>v.trim()||null;return {name:form.name.trim(),kind:form.kind,description:clean(form.description),status:form.status,statusReason:clean(form.statusReason),version};}
export function validSupplier(form:SupplierForm){return text(form.name.trim(),200)&&kinds.includes(form.kind)&&statuses.includes(form.status)&&optional(form.description.trim()||null,2000)&&optional(form.statusReason.trim()||null,1000)&&(!["INACTIVE","BLOCKED"].includes(form.status)||!!form.statusReason.trim());}

export type SupplierArea={id:string;tenantId:string;supplierId:string;propertyId:string;propertyName:string;categoryId:string;categoryName:string;serviceName:string};
export function readSupplierAreas(v:unknown,tenant:string,supplier:string):{items:SupplierArea[];page:number;pageSize:number;totalCount:number}{
 if(!object(v)||!integer(v.page,1)||!integer(v.pageSize,1)||Number(v.pageSize)>100||!integer(v.totalCount)||!Array.isArray(v.items)||v.items.length>Number(v.pageSize)||v.items.length>Number(v.totalCount))throw new Error("Invalid supplier areas");
 const items=v.items.map(x=>{if(!object(x)||!supplierId(x.id)||x.tenantId!==tenant||x.supplierId!==supplier||!supplierId(x.propertyId)||!supplierId(x.categoryId)||!text(x.propertyName,200)||!text(x.categoryName,100)||!text(x.serviceName,200))throw new Error("Invalid supplier area");return x as SupplierArea;});
 if(new Set(items.map(x=>x.id)).size!==items.length)throw new Error("Duplicate supplier area");return {...v,items} as {items:SupplierArea[];page:number;pageSize:number;totalCount:number};
}
