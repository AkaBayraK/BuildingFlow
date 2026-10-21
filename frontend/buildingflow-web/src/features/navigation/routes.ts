const guid="[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}";
export function propertyIdFromPath(path:string){return new RegExp(`^/properties/(${guid})(?:/|$)`,"i").exec(path)?.[1] ?? null;}
export type ManagementSection="overview"|"buildings"|"units"|"people"|"dues"|"payments"|"expenses"|"announcements"|"surveys"|"documents"|"meters"|"maintenance"|"reports"|"sms"|"requests"|"website";
export function managementSection(path:string,propertyId:string):ManagementSection{
 const suffix=path.slice(`/properties/${propertyId}`.length);
 return suffix==="/website"?"website":suffix==="/requests"?"requests":suffix==="/sms"?"sms":suffix==="/meters"?"meters":suffix==="/maintenance"?"maintenance":suffix==="/reports"?"reports":suffix==="/announcements"?"announcements":suffix==="/surveys"?"surveys":suffix==="/documents"?"documents":suffix==="/expenses"?"expenses":suffix==="/payments"?"payments":suffix==="/dues"?"dues":suffix.startsWith("/people")?"people":suffix==="/units"||/^\/buildings\/[^/]+\/units$/.test(suffix)?"units":suffix.startsWith("/buildings")?"buildings":"overview";
}
