export type Person = {id:string;propertyId:string;firstName:string;lastName:string;phoneNumber:string|null;email:string|null;isEnabled:boolean;version:number;createdAt:string;activeOccupancyCount:number;personKind?:"NATURAL"|"LEGAL"|null;citizenship?:"DOMESTIC"|"FOREIGN"|null;countryCode?:string|null;identityNumber?:string|null;birthDate?:string|null;gender?:"FEMALE"|"MALE"|"OTHER"|"UNSPECIFIED"|null;address?:string|null;linkedUserId?:string|null;propertyName?:string|null;units?:{unitId:string;number:string;buildingId:string|null;buildingName:string|null;role:OccupancyType}[]|null};
export type OccupancyType="OWNER"|"TENANT"|"RESIDENT";
export type Occupancy = {id:string;propertyId:string;personId:string;unitId:string;buildingId:string|null;personName:string;unitNumber:string;buildingName:string|null;type:OccupancyType;startDate:string;endDate:string|null;isPrimaryContact:boolean;status:"ACTIVE"|"UPCOMING"|"ENDED";version:number};
export type UnitChoice = {id:string;buildingId:string|null;number:string;buildingName:string|null;isEnabled:boolean};
export type CreateOccupancyRequest = {personId:string;unitId:string;type:OccupancyType;startDate:string;endDate:string|null;isPrimaryContact:boolean};
export const occupancyTypeNames:Record<OccupancyType,string>={OWNER:"owner",TENANT:"tenant",RESIDENT:"resident"};
export const occupancyStatusNames:Record<Occupancy["status"],string>={ACTIVE:"ongoing",UPCOMING:"planned",ENDED:"ended"};
export function personName(person:Person){return `${person.firstName} ${person.lastName}`.trim();}
