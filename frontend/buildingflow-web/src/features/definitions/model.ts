// TNM 01 sözleşmesi: GET /api/v1/definitions. Kod-sahipli liste kaydı; yalnız bilinen anahtar ve değerler gösterilir.
export const definitionsPath="/api/v1/definitions";
export const definitionRoles=["SUPER_ADMIN","SITE_ADMIN","MANAGER"] as const;
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

export type DefinitionList={key:DefinitionKey;group:DefinitionGroup;level:DefinitionLevel;source:string;plannedPhase:string|null;values:string[]};
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
  if(!oneOf(definitionGroups,row.group)||!oneOf(definitionLevels,row.level)||typeof row.source!=="string"||!phase(row.plannedPhase)||!Array.isArray(row.values)||!row.values.length||!row.values.every(code)||new Set(row.values).size!==row.values.length||(row.level==="FIXED")!==(row.plannedPhase===null))throw Error("Invalid definition list");
  return [{key:row.key,group:row.group,level:row.level,source:row.source,plannedPhase:row.plannedPhase as string|null,values:row.values as string[]}];
 });
 const catalogs=body.catalogs.flatMap(item=>{
  const row=record(item);
  if(!row||!oneOf(catalogKeys,row.key))return [];
  if(!oneOf(definitionGroups,row.group)||!oneOf(catalogScopes,row.scope)||!route(row.route))throw Error("Invalid definition catalog");
  return [{key:row.key,group:row.group,scope:row.scope,route:row.route as string|null}];
 });
 return {lists,catalogs};
}

export function definitionLabelKey(key:DefinitionKey,value:string):{namespace:string;key:string}{
 const label:{namespace:string;prefix?:string;key?:Record<string,string>}=definitionLabels[key];
 return {namespace:label.namespace,key:label.key?label.key[value]??value:`${label.prefix}${value}`};
}

export function filterLists(lists:readonly DefinitionList[],group:DefinitionGroup|"ALL",level:DefinitionLevel|"ALL"){
 return lists.filter(list=>(group==="ALL"||list.group===group)&&(level==="ALL"||list.level===level));
}
