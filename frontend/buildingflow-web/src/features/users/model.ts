export const allRoles=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT","RESIDENT"] as const;
export const siteRoles=["MANAGER","ACCOUNTANT","RESIDENT"] as const;
export type Role=typeof allRoles[number];
export type LinkedPerson={personId:string;propertyId:string;propertyName:string;fullName:string;version:number};
export type ManagedUser={id:string;username:string;email:string|null;firstName:string|null;lastName:string|null;enabled:boolean;emailVerified:boolean;createdAt:string|null;roles:Role[];propertyCount:number;linkedPeople:LinkedPerson[];canEdit:boolean;photoId?:string|null;canManagePhoto?:boolean;canEditEmail?:boolean;invitationStatus?:"ACCEPTED"|"UNAVAILABLE"|"NOT_REQUESTED"|null};
export type UserPage={items:ManagedUser[];page:number;pageSize:number;totalCount:number};
const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const guid=(v:unknown)=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const optional=(v:unknown)=>v===null||typeof v==="string";
const int=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
export function readUser(v:unknown):ManagedUser{
 if(!obj(v)||typeof v.id!=="string"||!v.id||typeof v.username!=="string"||!optional(v.email)||!optional(v.firstName)||!optional(v.lastName)||typeof v.enabled!=="boolean"||typeof v.emailVerified!=="boolean"||!(v.createdAt===null||typeof v.createdAt==="string"&&!Number.isNaN(Date.parse(v.createdAt)))||!Array.isArray(v.roles)||!v.roles.every(r=>(allRoles as readonly string[]).includes(String(r)))||!int(v.propertyCount)||typeof v.canEdit!=="boolean"||!(v.photoId===undefined||v.photoId===null||guid(v.photoId))||!(v.canManagePhoto===undefined||typeof v.canManagePhoto==="boolean")||!(v.canEditEmail===undefined||typeof v.canEditEmail==="boolean")||!(v.invitationStatus===undefined||v.invitationStatus===null||["ACCEPTED","UNAVAILABLE","NOT_REQUESTED"].includes(String(v.invitationStatus)))||!Array.isArray(v.linkedPeople))throw new Error("Invalid user");
 for(const p of v.linkedPeople)if(!obj(p)||!guid(p.personId)||!guid(p.propertyId)||typeof p.propertyName!=="string"||typeof p.fullName!=="string"||!int(p.version,1))throw new Error("Invalid linked person");
 return v as ManagedUser;
}
export function readUserPage(v:unknown):UserPage{
 if(!obj(v)||!int(v.page,1)||!int(v.pageSize,1)||!int(v.totalCount)||!Array.isArray(v.items)||v.items.length>Number(v.pageSize))throw new Error("Invalid user page");
 return {items:v.items.map(readUser),page:Number(v.page),pageSize:Number(v.pageSize),totalCount:Number(v.totalCount)};
}
export const displayName=(u:ManagedUser)=>[u.firstName,u.lastName].filter(Boolean).join(" ")||u.username;
export const validUsername=(value:string)=>/^[a-z0-9][a-z0-9._-]{2,49}$/.test(value);
export const strongPassword=(value:string)=>value.length>=12&&value.length<=128&&/[A-Z]/.test(value)&&/[a-z]/.test(value)&&/[0-9]/.test(value);

export type UserPhotoState={userId:string;photoId:string|null;photoSha256:string|null;version:number;canReplace:boolean};
export function readUserPhoto(v:unknown,target:string):UserPhotoState{
 if(!obj(v)||v.userId!==target||!int(v.version)||typeof v.canReplace!=="boolean"||!(v.photoId===null||guid(v.photoId))||!(v.photoSha256===null||typeof v.photoSha256==="string"&&/^[a-f0-9]{64}$/i.test(v.photoSha256))||((v.photoId===null)!==(v.photoSha256===null))||(v.photoId!==null&&!int(v.version,1)))throw new Error("Invalid user photo");
 return v as UserPhotoState;
}
export function readUserPhotoResult(v:unknown,target:string,operationId:string):UserPhotoState{
 if(!obj(v)||v.operationId!==operationId||typeof v.replayed!=="boolean")throw new Error("Invalid photo result");
 return readUserPhoto(v.photo,target);
}
