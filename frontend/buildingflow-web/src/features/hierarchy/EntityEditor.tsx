"use client";
import {PersonFormFields} from "@/features/people/PersonFormFields";
import {formatPhone,normalizeEmail,validEmail,validPhone} from "@/lib/contact";
import {personName as normalizedName} from "@/lib/person-names";
import {ManagementPanel} from "@/components/management/ManagementPanel";
import {MessageBanner,FieldError} from "@/components/feedback/MessageBanner";
import {useNotification} from "@/components/feedback/Notifications";
import {useLocale,useTranslations} from "next-intl";
import { useRef, useState } from "react";
import { Building2, Blocks, DoorOpen, Users, UserPlus, Info, LoaderCircle, RefreshCw, Save, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { ApiError, errorMessage, useAuthorizedApi, unitTypeNames, propertyTypeNames, type Property, type Building, type Unit, type UnitType, type PropertyType } from "@/services/api";
import {personName,type Person} from "@/features/people/types";
import {IdentityMark} from "@/components/management/WorkspacePresentation";
import {detailFields,detailValues,detailPayload,validateDetailValues} from "@/features/property-details/model";
import {DetailInputs} from "@/features/property-details/DetailFields";
import {parseProperty,parseBuilding,parseUnit,parsePerson} from "@/services/management-contracts";
export type Entity=Property|Building|Unit|Person;
export type EntityKind="property"|"building"|"unit"|"person";
const names={property:"property",building:"buildingBlock",unit:"unit",person:"person"};
type Field={key:string;label:string;max?:number;type?:string;optional?:boolean;placeholder?:string};
const fields:Record<EntityKind,Field[]>={
  property:[{key:"name",label:"propertyName",max:200},{key:"city",label:"city",max:100},{key:"district",label:"district",max:100},{key:"address",label:"address",max:500},{key:"floorCount",label:"numberOfFloors",type:"number",optional:true}],
  building:[{key:"name",label:"buildingBlockName",max:100,placeholder:"eGBlockA"},{key:"floorCount",label:"numberOfFloors",type:"number",optional:true}],
  unit:[{key:"number",label:"unitNumber",max:30,placeholder:"eG12OrA12"},{key:"floor",label:"floor",type:"number",placeholder:"use1ForTheBasement"},{key:"grossSquareMeters",label:"grossAreaM",type:"number",placeholder:"eG12050"}],
  person:[{key:"firstName",label:"firstName",max:100},{key:"lastName",label:"lastName",max:100},{key:"phoneNumber",label:"phone",max:30,type:"tel"},{key:"email",label:"email",max:254,type:"email"}]
};
function initialValues(kind:EntityKind,target:Entity|null){const values:Record<string,string>={};const source=target as unknown as Record<string,unknown>|null;for(const field of fields[kind])values[field.key]=source?.[field.key]==null?"":String(source[field.key]);if(kind==="person")for(const [key,fallback] of Object.entries({personKind:"NATURAL",citizenship:"DOMESTIC",countryCode:"TR",identityNumber:"",birthDate:"",gender:"",address:"",linkedUserId:""}))values[key]=source?.[key]==null?fallback:String(source[key]);return {...values,...detailValues(target,kind)};}
export function EntityEditor({kind,url,target,apartmentOnly=false,focusReturnTarget,onClose,onSaved}:{kind:EntityKind;url:string;target:Entity|null;apartmentOnly?:boolean;focusReturnTarget?:HTMLElement|null;onClose:()=>void;onSaved:(saved:unknown)=>void}) {
  const t=useTranslations("ui");const codes=useTranslations("apiErrors"); const locale=useLocale();const notification=useNotification();

  const api=useAuthorizedApi();const [record,setRecord]=useState(target);const [values,setValues]=useState(()=>initialValues(kind,target));
  const [enabled,setEnabled]=useState(target?.isEnabled ?? true);const [unitType,setUnitType]=useState<UnitType>(target && "number" in target?target.type:"APARTMENT");
  const [propertyType,setPropertyType]=useState<PropertyType>(target && "city" in target?target.type:"APARTMENT");
  const [errors,setErrors]=useState<Record<string,string[]>>({});const [error,setError]=useState("");const [conflict,setConflict]=useState(false);const [busy,setBusy]=useState(false);const lock=useRef(false);
  const personFields:Field[]=kind!=="person"?[]:[{key:"countryCode",label:"personCountry",max:2},{key:"identityNumber",label:values.citizenship==="FOREIGN"?(values.personKind==="LEGAL"?"personForeignCompanyId":"personPassport"):values.personKind==="LEGAL"?"personTaxId":"personNationalId",max:50},...(values.personKind==="NATURAL"?[{key:"birthDate",label:"personBirthDate",type:"date",optional:true}]:[]),{key:"address",label:"address",max:500,optional:true},{key:"linkedUserId",label:"personLinkedAccount",max:200,optional:true}];
  const formFields=[...fields[kind],...personFields].filter(field=>kind!=="person"||field.key!=="lastName"||values.personKind!=="LEGAL").filter(field=>field.key!=="linkedUserId").map(field=>({...field,label:apartmentOnly && field.key==="number"?t("apartmentNumber"):t(kind==="person"&&field.key==="firstName"&&values.personKind==="LEGAL"?"personCompanyName":field.label),placeholder:field.placeholder?t(field.placeholder):undefined})).filter(field=>field.key!=="floorCount" || kind!=="property" || propertyType==="APARTMENT");
  function changePersonField(key:string,value:string){setValues(previous=>({...previous,[key]:value}));setErrors(previous=>({...previous,[key]:[]}));}
  function changePersonClassification(key:string,value:string){setValues(previous=>({...previous,[key]:value,...(key==="citizenship"?{countryCode:value==="DOMESTIC"?"TR":"",identityNumber:""}:key==="personKind"?{identityNumber:"",birthDate:"",gender:""}:{})}));setErrors({});}
  const label=kind==="unit" && apartmentOnly?t("apartment"):t(names[kind]);
  function decode(value:unknown):Entity{const parts=url.split("/");const parsed=kind==="property"?parseProperty(value,target?.id):kind==="unit"?parseUnit(value,parts[4],parts[5]==="buildings"?parts[6]:null):kind==="building"?parseBuilding(value,parts[4]):parsePerson(value,parts[4]);if(target&&parsed.id!==target.id)throw new Error("Invalid entity identity");return parsed;}
  async function reload(){if(lock.current || !target)return;lock.current=true;setBusy(true);try{const latest=decode(await api<unknown>(url+"/"+target.id));setRecord(latest);setValues(initialValues(kind,latest));setEnabled(latest.isEnabled);if("number" in latest)setUnitType(latest.type);if("city" in latest)setPropertyType(latest.type);setErrors({});setError("");setConflict(false);}catch(error){setError(errorMessage(error,locale));setConflict(error instanceof ApiError && error.status===409);}finally{lock.current=false;setBusy(false);}}
  async function save(event:React.FormEvent){
    event.preventDefault();if(lock.current)return;const validation:Record<string,string[]>={};const body:Record<string,unknown>={};
    for(const field of formFields){const value=values[field.key].trim();
      if(!value && !field.optional)validation[field.key]=[t("requiredField",{field:field.label})];else if(field.max && value.length>field.max)validation[field.key]=[t("maxCharacters",{max:field.max})];
      if(field.type==="email" && value && !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value))validation[field.key]=[t("enterAValidEmailAddress")];
      if(field.type==="number"){
        if(!value && field.optional){body[field.key]=null;continue;}const number=Number(value);body[field.key]=number;
        if(value && (!Number.isFinite(number) || ((field.key==="floor" || field.key==="floorCount") && (!Number.isInteger(number) || number < -2147483648 || number > 2147483647))))validation[field.key]=[t("enterAValidInteger")];
        if(value && field.key==="floorCount" && number<=0)validation[field.key]=[t("theNumberOfFloorsMustBePositive")];
        if(value && field.key==="grossSquareMeters" && (!Number.isFinite(number) || number<=0 || number>99999999.99 || Math.abs(Math.round(number*100)-number*100)>0.000001))validation[field.key]=[t("areaMustBePositiveWithAtMostTwoDecimal")];
      }else body[field.key]=field.optional && !value?null:value;
    }
    if(kind==="person"){
      body.linkedUserId=values.linkedUserId.trim()||null;body.personKind=values.personKind;body.citizenship=values.citizenship;body.countryCode=values.citizenship==="DOMESTIC"?"TR":values.countryCode.toUpperCase();body.lastName=values.personKind==="LEGAL"?"":body.lastName;Object.assign(body,normalizedName(String(body.firstName??""),String(body.lastName??""),values.personKind,values.citizenship==="FOREIGN"?"FOREIGN":"DOMESTIC"));body.birthDate=values.personKind==="LEGAL"?null:body.birthDate;body.gender=values.personKind==="LEGAL"?null:values.gender||null;
      if(!/^[A-Z]{2}$/.test(String(body.countryCode))||values.citizenship==="FOREIGN"&&body.countryCode==="TR")validation.countryCode=[codes("BF_PERSON_PROFILE_INVALID")];
      const id=String(body.identityNumber??"");if(values.citizenship==="DOMESTIC"?!(values.personKind==="LEGAL"?/^[0-9]{10}$/:/^[1-9][0-9]{10}$/).test(id):values.personKind==="NATURAL"&&!/^[A-Za-z0-9-]{6,30}$/.test(id))validation.identityNumber=[codes("BF_PERSON_PROFILE_INVALID")];
      // İŞ19: the shared contact rules (+90 and 3-3-2-2 for Turkey, + country code elsewhere; lower-case e-mail).
      const phone=formatPhone(String(body.phoneNumber??""));body.phoneNumber=phone;if(!validPhone(phone))validation.phoneNumber=[t("phoneFieldInvalid")];
      if(body.email!=null&&body.email!==""){body.email=normalizeEmail(String(body.email));if(!validEmail(String(body.email)))validation.email=[t("emailFieldInvalid")];}
    }
    if(kind==="unit")body.type=apartmentOnly?"APARTMENT":unitType;
    if(kind==="property"){body.type=propertyType;if(propertyType!=="APARTMENT")body.floorCount=null;}
    Object.assign(body,detailPayload(values,kind,propertyType));
    for(const [key,code] of Object.entries(validateDetailValues(values,kind,propertyType,Number(body.floor),Number(body.grossSquareMeters))))validation[key]=[code.startsWith("BF_")?codes(code):t(code)];
    setErrors(validation);setError("");setConflict(false);if(Object.keys(validation).length){document.getElementById(`entity-${Object.keys(validation)[0]}`)?.focus();return;}
    if(record){body.version=record.version;body.isEnabled=enabled;}lock.current=true;setBusy(true);
    try{const saved=decode(await api<unknown>(url+(record?"/"+record.id:""),{method:record?"PUT":"POST",body:JSON.stringify(body)}));notification.success(t("entitySaved",{entity:label}));onSaved(saved);onClose();}
    catch(error){setError(errorMessage(error,locale));if(error instanceof ApiError){setErrors(error.errors);setConflict(error.status===409);}}finally{lock.current=false;setBusy(false);}
  }
  return <Dialog open onOpenChange={open=>{if(!open && !busy)onClose();}}><ManagementPanel className={kind==="person"?"person-editor-panel":undefined} focusReturnTarget={focusReturnTarget} showCloseButton={!busy}>{kind==="person"?<DialogHeader className="person-editor-header"><div className="flex items-start gap-4"><IdentityMark icon={target?Users:UserPlus} className="size-12 shrink-0"/><div><DialogTitle>{target?t("editEntity",{entity:label}):t("personCreateTitle")}</DialogTitle><DialogDescription className="mt-1">{t("personCreateSubtitle")}</DialogDescription></div></div></DialogHeader>:<DialogHeader className="border-b pb-5 pr-6 text-left"><IdentityMark icon={kind==="property"?Building2:kind==="building"?Blocks:kind==="unit"?DoorOpen:Users} className="mb-2 size-12"/><DialogTitle className="text-2xl font-bold">{target?t("editEntity",{entity:label}):t("newEntity",{entity:label})}</DialogTitle><DialogDescription>{kind==="unit"?t("theNumberMustBeUniqueWithinThisScopeUse"):kind==="building"?t("theBuildingBlockNameMustBeUniqueWithinThis"):t("updateTheNameLocationAndActiveStatusTheProperty")}</DialogDescription></DialogHeader>}
    {error && <MessageBanner className="my-4 text-left" kind={conflict?"warning":"error"}>{error}{conflict && record && <Button size="sm" variant="view" className="mt-3 bg-card" disabled={busy} onClick={()=>void reload()}><RefreshCw className="size-3"/>{t("loadCurrentRecord")}</Button>}</MessageBanner>}
    <form noValidate onSubmit={save} className={kind==="person"?"person-editor-form":undefined}><fieldset disabled={busy} className={kind==="person"?"person-editor-fieldset":"space-y-5 border-0 p-0"}>
    {kind==="property" && record && "city" in record && <div className="space-y-2"><Label htmlFor="entity-type">{t("propertyType")}</Label><select id="entity-type" className="h-11 w-full rounded-md border bg-card px-3 text-sm disabled:bg-muted disabled:text-muted-foreground" value={propertyType} disabled={!record.canChangeType} aria-describedby="entity-type-help" aria-invalid={!!errors.type?.length} onChange={event=>{setPropertyType(event.target.value as PropertyType);setErrors(previous=>({...previous,type:[],floorCount:[]}));}}>{(Object.keys(propertyTypeNames) as PropertyType[]).map(type=><option key={type} value={type}>{t(propertyTypeNames[type])}</option>)}</select><MessageBanner id="entity-type-help" kind={record.canChangeType?"info":"warning"}>{record.canChangeType?t("noBuildingsOrUnitsHaveBeenRecordedTheType"):t("buildingsUnitsOrTheirHistoryExistTheTypeCannot")}</MessageBanner>{errors.type?.length?<FieldError>{errors.type.join(" ")}</FieldError>:null}</div>}
    {kind==="unit" && !apartmentOnly && <div className="space-y-2"><Label htmlFor="unit-type">{t("unitType")}</Label><select id="unit-type" className="h-11 w-full rounded-md border bg-card px-3 text-sm" value={unitType} onChange={event=>setUnitType(event.target.value as UnitType)}>{(Object.keys(unitTypeNames) as UnitType[]).map(type=><option key={type} value={type}>{t(unitTypeNames[type])}</option>)}</select></div>}
    {kind==="person"&&<PersonFormFields values={values} errors={errors} url={url} busy={busy} onChange={changePersonField} onClassification={changePersonClassification}/>}
    {kind!=="person"&&formFields.map(field=><div key={field.key} className="space-y-2"><Label htmlFor={`entity-${field.key}`}>{field.label}{field.optional?t("optional"):" *"}</Label>{field.key==="address"?<Textarea id={`entity-${field.key}`} required={!field.optional} maxLength={field.max} value={values[field.key]} aria-invalid={!!errors[field.key]?.length} aria-describedby={errors[field.key]?.length?`entity-error-${field.key}`:undefined} onChange={event=>{setValues(previous=>({...previous,[field.key]:event.target.value}));setErrors(previous=>({...previous,[field.key]:[]}));}}/>:<Input id={`entity-${field.key}`} type={field.type ?? "text"} step={field.key==="grossSquareMeters"?"0.01":"1"} required={!field.optional} maxLength={field.max} value={values[field.key]} placeholder={field.placeholder} aria-invalid={!!errors[field.key]?.length} aria-describedby={errors[field.key]?.length?`entity-error-${field.key}`:undefined} onChange={event=>{setValues(previous=>({...previous,[field.key]:event.target.value}));setErrors(previous=>({...previous,[field.key]:[]}));}} className="h-11 bg-card"/>}{errors[field.key]?.length?<FieldError id={`entity-error-${field.key}`}>{errors[field.key].join(" ")}</FieldError>:null}</div>)}

    {!!detailFields(kind,propertyType).length&&<section className="space-y-4 border-t pt-5"><h3 className="text-sm font-semibold">{t(kind==="unit"?"unitDetailFeatures":"buildingDetailFeatures")}</h3><DetailInputs fields={detailFields(kind,propertyType)} values={values} errors={errors} onChange={(key,value)=>{setValues(previous=>({...previous,[key]:value}));setErrors(previous=>({...previous,[key]:[]}));}}/>{kind==="unit"&&<p className="text-xs leading-5 text-muted-foreground">{t("floorPositionHelp")}</p>}</section>}
    {record && <label className="flex cursor-pointer items-center gap-3 rounded-xl border bg-muted p-4 text-sm"><input type="checkbox" checked={enabled} onChange={event=>setEnabled(event.target.checked)} className="size-4 accent-primary"/><span><strong>{t("activeRecord")}</strong><span className="mt-1 block text-xs text-muted-foreground">{kind==="unit"?t("inactiveRecordsRemainVisibleInTheList"):t("childRecordsCannotChangeUnderAnInactiveParent")}</span></span></label>}
    <DialogFooter className={kind==="person"?"person-editor-footer":"border-t pt-5"}>{kind==="person"&&<p className="person-editor-required"><Info aria-hidden="true" className="size-4 shrink-0"/>{t("personRequiredNote")}</p>}<div className="flex shrink-0 justify-end gap-2"><Button type="button" variant="outline" onClick={onClose} disabled={busy}>{t("cancel")}</Button><Button type="submit" disabled={busy}>{busy?<LoaderCircle className="size-4 animate-spin"/>:<Save className="size-4"/>}{busy?t("saving"):kind==="person"?t("personSaveButton"):t("save")}</Button></div></DialogFooter></fieldset></form>
  </ManagementPanel></Dialog>;
}
export function DeleteEntity({kind,url,target,onClose,onDeleted}:{kind:EntityKind;url:string;target:Entity;onClose:()=>void;onDeleted:()=>void}) {
  const t=useTranslations("ui"); const locale=useLocale();const notification=useNotification();

  const [conflict,setConflict]=useState(false);const api=useAuthorizedApi();const [error,setError]=useState("");const [busy,setBusy]=useState(false);const lock=useRef(false);const label="firstName" in target?personName(target):"name" in target?target.name:target.number;
  async function remove(){if(lock.current)return;lock.current=true;setBusy(true);setError("");try{await api<void>(`${url}/${target.id}?version=${target.version}`,{method:"DELETE"});notification.success(t("entityDeleted",{entity:t(names[kind])}));onDeleted();onClose();}catch(error){setError(errorMessage(error,locale));setConflict(error instanceof ApiError && error.status===409);}finally{lock.current=false;setBusy(false);}}
  return <Dialog open onOpenChange={open=>{if(!open && !busy)onClose();}}><DialogContent showCloseButton={!busy} className="rounded-2xl bg-card"><DialogHeader><DialogTitle>{t("deleteEntity",{entity:t(names[kind])})}</DialogTitle><DialogDescription>{t("deleteDescription",{name:label})} {kind==="person"?t("ongoingOrPlannedUnitRelationshipsMustEndFirst"):kind!=="unit"?t("deleteTheChildRecordsFirst"):t("ongoingOrPlannedPersonRelationshipsMustEndFirst")}</DialogDescription></DialogHeader>{error && <MessageBanner className="my-4 text-left" kind={conflict?"warning":"error"}>{error} {t("closeThisDialogAndRefreshTheList")}</MessageBanner>}<DialogFooter><Button variant="outline" disabled={busy} onClick={onClose}>{t("cancel")}</Button><Button variant="destructive" disabled={busy} onClick={()=>void remove()}>{busy?<LoaderCircle className="size-4 animate-spin"/>:<Trash2 className="size-4"/>}{t("delete")}</Button></DialogFooter></DialogContent></Dialog>;
}
