import type {Unit} from '../../services/api';
export type UnitFilter={unitId?:string;invalid:boolean};
export type UnitPersonLink={personId:string;personName:string};
export type UnitParties={unitId:string;buildingId:string|null;owners:UnitPersonLink[];tenants:UnitPersonLink[]};
export type UnitPartiesResponse={propertyId:string;asOfDate:string;items:UnitParties[]};
const guid=/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
function validId(value:unknown):value is string{return typeof value==='string'&&guid.test(value)&&value!=='00000000-0000-0000-0000-000000000000';}
export function readUnitFilter(value:string|string[]|undefined):UnitFilter{return value===undefined?{invalid:false}:validId(value)?{unitId:value.toLowerCase(),invalid:false}:{invalid:true};}
export function unitListHref(row:{propertyId:string;buildingId:string|null;unitId:string}){if(!validId(row.propertyId)||!validId(row.unitId)||(row.buildingId!==null&&!validId(row.buildingId)))throw new Error('Invalid unit link');return `/properties/${row.propertyId}${row.buildingId?`/buildings/${row.buildingId}`:''}/units?unitId=${row.unitId}`;}
export function readUnitParties(value:unknown,propertyId:string,units:Pick<Unit,'id'|'buildingId'>[]):UnitPartiesResponse{
 if(!value||typeof value!=='object'||Array.isArray(value))throw new Error('Invalid parties response');
 const row=value as Record<string,unknown>;const expected=new Map(units.map(unit=>[unit.id,unit.buildingId]));
 if(!validId(propertyId)||row.propertyId!==propertyId||typeof row.asOfDate!=='string'||!/^\d{4}-\d{2}-\d{2}$/.test(row.asOfDate)||!Number.isFinite(Date.parse(row.asOfDate))||new Date(row.asOfDate).toISOString().slice(0,10)!==row.asOfDate||!Array.isArray(row.items)||row.items.length!==units.length||expected.size!==units.length)throw new Error('Invalid parties scope');
 const seen=new Set<string>();for(const item of row.items){if(!item||typeof item!=='object'||Array.isArray(item))throw new Error('Invalid unit parties');const r=item as Record<string,unknown>;
  if(!validId(r.unitId)||!expected.has(r.unitId)||r.buildingId!==expected.get(r.unitId)||seen.has(r.unitId))throw new Error('Invalid unit parties scope');seen.add(r.unitId);
  for(const key of ['owners','tenants']){if(!Array.isArray(r[key]))throw new Error('Invalid person links');const ids=new Set<string>();for(const link of r[key]){if(!link||typeof link!=='object'||Array.isArray(link))throw new Error('Invalid person link');const p=link as Record<string,unknown>;if(!validId(p.personId)||typeof p.personName!=='string'||!p.personName.trim()||p.personName.length>201||ids.has(p.personId))throw new Error('Invalid person link');ids.add(p.personId);}}
 }
 return value as UnitPartiesResponse;
}
