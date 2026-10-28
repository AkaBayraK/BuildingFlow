export type ReportDay={id:string;day:string;entries:number;exits:number;reservations:number};
export type ParkingReport={propertyId:string;from:string;through:string;asOf:string;capacity:number;allocatedSpaces:number;reservedGuestSpaces:number;manualGuestsInside:number;overdueGuests:number;scheduledOperationsEnabled:boolean;hardwareEnabled:boolean;days:ReportDay[]};
const date=(v:unknown):v is string=>typeof v==='string'&&/^\d{4}-\d{2}-\d{2}$/.test(v)&&Number.isFinite(Date.parse(v))&&new Date(v).toISOString().slice(0,10)===v;
export function readReport(value:unknown,propertyId:string,from:string,through:string):ParkingReport{
 const fail=():never=>{throw new Error('Invalid parking report');};
 if(!value||typeof value!=='object')return fail();const v=value as Record<string,unknown>;
 if(v.propertyId!==propertyId||v.from!==from||v.through!==through||!date(from)||!date(through)||typeof v.asOf!=='string'||!Number.isFinite(Date.parse(v.asOf))||typeof v.scheduledOperationsEnabled!=='boolean'||v.hardwareEnabled!==false||!Array.isArray(v.days))return fail();
 for(const key of ['capacity','allocatedSpaces','reservedGuestSpaces','manualGuestsInside','overdueGuests'])if(!Number.isSafeInteger(v[key])||(v[key] as number)<0)return fail();
 const length=(Date.parse(through)-Date.parse(from))/86400000+1;if(length<1||length>366||v.days.length!==length)return fail();
 const days=v.days.map((raw:unknown,i:number)=>{if(!raw||typeof raw!=='object')return fail();const d=raw as Record<string,unknown>;
  if(!date(d.day)||d.day!==new Date(Date.parse(from)+i*86400000).toISOString().slice(0,10))return fail();
  for(const k of ['entries','exits','reservations'])if(!Number.isSafeInteger(d[k])||(d[k] as number)<0)return fail();
  return {id:d.day,day:d.day,entries:d.entries as number,exits:d.exits as number,reservations:d.reservations as number};});
 return {...v,days} as ParkingReport;
}
// Only validated ISO dates and nonnegative integers: no user text, plates or CSV formula cells.
export function reportCsv(report:ParkingReport):string{
 const checked=readReport(report,report.propertyId,report.from,report.through);
 return '\ufeffday,manual_entries,manual_exits,reservations_started\r\n'+checked.days.map(d=>`${d.day},${d.entries},${d.exits},${d.reservations}`).join('\r\n')+'\r\n';
}
