// TNM 01/02 sözleşmesi: GET /api/v1/definitions ve PUT .../{list}/values/{code}. Kod-sahipli liste kaydı; yalnız bilinen
// anahtar ve değerler gösterilir. Sistem geneli ad/ikon/ton/sıra/aktiflik yalnız SUPER_ADMIN tarafından yazılır.
export const definitionsPath="/api/v1/definitions";
export const definitionRoles=["SUPER_ADMIN","SITE_ADMIN","MANAGER"] as const;
export const definitionEditorRole="SUPER_ADMIN";
export const definitionValuePath=(list:string,code:string)=>`${definitionsPath}/${encodeURIComponent(list)}/values/${encodeURIComponent(code)}`;
// Backend DefinitionRules.IconKeys ve Tones ile aynı; serbest metin ikon kabul edilmez.
export const definitionIconKeys=["wrench","droplet","zap","flame","paint-roller","hammer","sparkles","shield-check","building-2","lightbulb","ellipsis","megaphone","calendar-check-2","car-front","bike","truck","trees","dog","users","coins","concierge-bell","party-popper","square-parking","messages-square","waves","dumbbell","presentation","baby","flower-2","trash-2","package","siren","bell","clipboard-check","leaf","key-round"] as const;
export const definitionTones=["blue","green","amber","teal","red","violet","neutral"] as const;
export type DefinitionIconKey=typeof definitionIconKeys[number];
export type DefinitionTone=typeof definitionTones[number];
export const maxDefinitionName=100,maxDefinitionOrder=999;
export const definitionGroups=["PROPERTY","UNIT","FINANCE","REQUEST","MAINTENANCE","PARKING","SUPPLIER","COMMUNICATION","DOCUMENT"] as const;
export const definitionLevels=["FIXED","SYSTEM","PROPERTY"] as const;
export const catalogScopes=["SYSTEM","TENANT","PROPERTY"] as const;
export type DefinitionGroup=typeof definitionGroups[number];
export type DefinitionLevel=typeof definitionLevels[number];
export type CatalogScope=typeof catalogScopes[number];

// Etiket kaynağı mevcut modül sözlükleridir; Tanımlar aynı metni ikinci kez tanımlamaz.
export const definitionLabels={
 PROPERTY_TYPE:{namespace:"ui",key:{APARTMENT:"apartmentBuilding",SITE:"community",RESIDENCE:"residence",MIXED:"mixedUse"}},
 COMMON_AREA_TYPE:{namespace:"commonAreas",prefix:"type_"},
 UNIT_TYPE:{namespace:"ui",key:{APARTMENT:"apartment",OFFICE:"office",SHOP:"shop",VILLA:"villa",WAREHOUSE:"warehouse",WORKSHOP:"workshop"}},
 CHARGE_KIND:{namespace:"dues",prefix:"kind_"},
 CHARGE_CATEGORY:{namespace:"dues",prefix:"category_"},
 EXPENSE_CATEGORY:{namespace:"expenses",prefix:""},
 PAYMENT_METHOD:{namespace:"payments",prefix:""},
 REQUEST_CATEGORY:{namespace:"requests",prefix:"category_"},
 REQUEST_PRIORITY:{namespace:"requests",prefix:"priority_"},
 MAINTENANCE_TYPE:{namespace:"maintenance",prefix:"type_"},
 MAINTENANCE_FREQUENCY:{namespace:"suppliers",prefix:"frequency_"},
 VEHICLE_TYPE:{namespace:"parking",prefix:"type_"},
 PARKING_SPACE_TYPE:{namespace:"parking",prefix:"space_"},
 SUPPLIER_KIND:{namespace:"suppliers",prefix:""},
 ANNOUNCEMENT_CATEGORY:{namespace:"communication",prefix:"category_"},
 SURVEY_CATEGORY:{namespace:"surveys",prefix:"category_"},
} as const satisfies Record<string,{namespace:string;prefix:string}|{namespace:string;key:Record<string,string>}>;
export type DefinitionKey=keyof typeof definitionLabels;
export const definitionKeys=Object.keys(definitionLabels) as DefinitionKey[];
export const catalogKeys=["SUPPLIER_CATEGORIES","DUES_CANCELLATION_REASONS","CONSENT_TYPES","SMS_TEMPLATES","PAYMENT_PROVIDERS"] as const;
export type CatalogKey=typeof catalogKeys[number];

export type DefinitionValue={code:string;nameTr:string|null;nameEn:string|null;iconKey:DefinitionIconKey|null;tone:DefinitionTone|null;sortOrder:number;isEnabled:boolean;locked:boolean;version:number};
export type DefinitionList={key:DefinitionKey;group:DefinitionGroup;level:DefinitionLevel;source:string;plannedPhase:string|null;editable:boolean;values:DefinitionValue[]};
export type DefinitionCatalog={key:CatalogKey;group:DefinitionGroup;scope:CatalogScope;route:string|null};
export type Definitions={lists:DefinitionList[];catalogs:DefinitionCatalog[]};

const record=(value:unknown)=>value&&typeof value==="object"&&!Array.isArray(value)?value as Record<string,unknown>:null;
const oneOf=<T extends string>(values:readonly T[],value:unknown):value is T=>values.includes(value as T);
const code=(value:unknown):value is string=>typeof value==="string"&&/^[A-Z][A-Z0-9_]{0,39}$/.test(value);
const phase=(value:unknown)=>value===null||typeof value==="string"&&/^TNM_\d{2}$/.test(value);
const route=(value:unknown)=>value===null||typeof value==="string"&&/^\/[a-z-]+$/.test(value);

export function readDefinitions(value:unknown):Definitions{
 const body=record(value);
 if(!body||!Array.isArray(body.lists)||!Array.isArray(body.catalogs))throw Error("Invalid definitions shape");
 const lists=body.lists.flatMap(item=>{
  const row=record(item);
  if(!row||!oneOf(definitionKeys,row.key))return [];
  if(!oneOf(definitionGroups,row.group)||!oneOf(definitionLevels,row.level)||typeof row.source!=="string"||!phase(row.plannedPhase)||typeof row.editable!=="boolean"||!Array.isArray(row.values)||!row.values.length||(row.level==="FIXED")!==(row.plannedPhase===null)||row.level==="FIXED"&&row.editable)throw Error("Invalid definition list");
  const values=row.values.map(readDefinitionValue);
  if(new Set(values.map(v=>v.code)).size!==values.length||!row.editable&&values.some(v=>v.version!==0))throw Error("Invalid definition list");
  return [{key:row.key,group:row.group,level:row.level,source:row.source,plannedPhase:row.plannedPhase as string|null,editable:row.editable,values}];
 });
 const catalogs=body.catalogs.flatMap(item=>{
  const row=record(item);
  if(!row||!oneOf(catalogKeys,row.key))return [];
  if(!oneOf(definitionGroups,row.group)||!oneOf(catalogScopes,row.scope)||!route(row.route))throw Error("Invalid definition catalog");
  return [{key:row.key,group:row.group,scope:row.scope,route:row.route as string|null}];
 });
 return {lists,catalogs};
}

const name=(value:unknown)=>value===null||typeof value==="string"&&value.trim().length>0&&value.length<=maxDefinitionName;
export function readDefinitionValue(value:unknown):DefinitionValue{
 const row=record(value);
 if(!row||!code(row.code)||!name(row.nameTr)||!name(row.nameEn)||!(row.iconKey===null||oneOf(definitionIconKeys,row.iconKey))||!(row.tone===null||oneOf(definitionTones,row.tone))||!Number.isInteger(row.sortOrder)||(row.sortOrder as number)<0||(row.sortOrder as number)>maxDefinitionOrder||typeof row.isEnabled!=="boolean"||typeof row.locked!=="boolean"||!Number.isSafeInteger(row.version)||(row.version as number)<0||row.locked&&!row.isEnabled)throw Error("Invalid definition value");
 return {code:row.code,nameTr:row.nameTr as string|null,nameEn:row.nameEn as string|null,iconKey:row.iconKey as DefinitionIconKey|null,tone:row.tone as DefinitionTone|null,sortOrder:row.sortOrder as number,isEnabled:row.isEnabled,locked:row.locked,version:row.version as number};
}

export function sortedValues(values:readonly DefinitionValue[]){return values.map((v,i)=>({v,i})).sort((a,b)=>a.v.sortOrder-b.v.sortOrder||a.i-b.i).map(x=>x.v);}
export function overrideName(value:DefinitionValue,locale:string){return (locale.startsWith("en")?value.nameEn:value.nameTr)??null;}

export type DefinitionForm={nameTr:string;nameEn:string;iconKey:DefinitionIconKey|"";tone:DefinitionTone|"";sortOrder:string;isEnabled:boolean};
export const definitionForm=(v:DefinitionValue):DefinitionForm=>({nameTr:v.nameTr??"",nameEn:v.nameEn??"",iconKey:v.iconKey??"",tone:v.tone??"",sortOrder:String(v.sortOrder),isEnabled:v.isEnabled});
export function definitionFormErrors(form:DefinitionForm,locked:boolean){
 const errors:string[]=[];
 if(form.nameTr.trim().length>maxDefinitionName)errors.push("nameTr");
 if(form.nameEn.trim().length>maxDefinitionName)errors.push("nameEn");
 if(!/^\d{1,3}$/.test(form.sortOrder.trim()))errors.push("sortOrder");
 if(locked&&!form.isEnabled)errors.push("isEnabled");
 return errors;
}
export const definitionBody=(form:DefinitionForm,version:number)=>({nameTr:form.nameTr.trim()||null,nameEn:form.nameEn.trim()||null,iconKey:form.iconKey||null,tone:form.tone||null,sortOrder:Number(form.sortOrder.trim()),isEnabled:form.isEnabled,version});

export function definitionLabelKey(key:DefinitionKey,value:string):{namespace:string;key:string}{
 const label:{namespace:string;prefix?:string;key?:Record<string,string>}=definitionLabels[key];
 return {namespace:label.namespace,key:label.key?label.key[value]??value:`${label.prefix}${value}`};
}

export function filterLists(lists:readonly DefinitionList[],group:DefinitionGroup|"ALL",level:DefinitionLevel|"ALL"){
 return lists.filter(list=>(group==="ALL"||list.group===group)&&(level==="ALL"||list.level===level));
}

// TNM 03: yönetim alanının etkin listeleri (formlar) ve alan ayarı (gizle/sırala).
export const propertyDefinitionsPath=(property:string)=>`/api/v1/properties/${encodeURIComponent(property)}/definitions`;
export const propertyDefinitionValuePath=(property:string,list:string,code:string)=>`${propertyDefinitionsPath(property)}/${encodeURIComponent(list)}/values/${encodeURIComponent(code)}`;
export type PropertyDefinitionValue={code:string;nameTr:string|null;nameEn:string|null;iconKey:DefinitionIconKey|null;tone:DefinitionTone|null;sortOrder:number;isEnabled:boolean;isHidden:boolean;selectable:boolean;locked:boolean;version:number};
export type PropertyDefinitionList={key:DefinitionKey;group:DefinitionGroup;level:DefinitionLevel;values:PropertyDefinitionValue[]};
export type PropertyDefinitions={propertyId:string;lists:PropertyDefinitionList[]};

export function readPropertyDefinitionValue(value:unknown):PropertyDefinitionValue{
 const row=record(value);
 if(!row||typeof row.isHidden!=="boolean"||typeof row.selectable!=="boolean")throw Error("Invalid area definition value");
 const base=readDefinitionValue({...row,version:row.version});
 if(base.locked&&(row.isHidden||!row.selectable)||row.selectable!==(base.locked||base.isEnabled&&!row.isHidden))throw Error("Invalid area definition value");
 return {...base,isHidden:row.isHidden,selectable:row.selectable};
}

export function readPropertyDefinitions(value:unknown,property:string):PropertyDefinitions{
 const body=record(value);
 if(!body||body.propertyId!==property||!Array.isArray(body.lists))throw Error("Invalid area definitions");
 const lists=body.lists.flatMap(item=>{
  const row=record(item);
  if(!row||!oneOf(definitionKeys,row.key))return [];
  if(!oneOf(definitionGroups,row.group)||!oneOf(definitionLevels,row.level)||row.level==="FIXED"||!Array.isArray(row.values)||!row.values.length)throw Error("Invalid area definition list");
  const values=row.values.map(readPropertyDefinitionValue);
  if(new Set(values.map(v=>v.code)).size!==values.length)throw Error("Invalid area definition list");
  return [{key:row.key,group:row.group,level:row.level,values}];
 });
 return {propertyId:property,lists};
}

// Form seçenekleri: sunucu sırası, seçilebilir değerler ve kaydın mevcut kodu (gizli/pasif olsa da düzenlemede korunur).
// Liste okunamazsa modülün kendi sabit listesi aynen kullanılır; yazım kuralını backend uygular.
export function definitionOptions<T extends string>(codes:readonly T[],list:PropertyDefinitionList|undefined,current?:string|null):T[]{
 if(!list)return [...codes];
 const known=list.values.filter(v=>codes.includes(v.code as T));
 const ordered=known.filter(v=>v.selectable||v.code===current).map(v=>v.code as T);
 return [...ordered,...codes.filter(c=>!known.some(v=>v.code===c))];
}
