export type Locale="tr"|"en";
const tags:Record<Locale,string>={tr:"tr-TR",en:"en-US"};
export function formatDateOnly(value:string,locale:Locale="tr") {
  // A calendar date has no offset. UTC display preserves its day in every locale.
  const date=new Date(`${value}T00:00:00Z`);
  return Number.isNaN(date.getTime())?"—":new Intl.DateTimeFormat(tags[locale],{timeZone:"UTC",year:"numeric",month:"2-digit",day:"2-digit"}).format(date);
}
export function businessToday(){
  const parts=new Intl.DateTimeFormat("en-CA",{timeZone:"Europe/Istanbul",year:"numeric",month:"2-digit",day:"2-digit"}).formatToParts(new Date());
  const part=(name:string)=>parts.find(value=>value.type===name)?.value ?? "";
  return `${part("year")}-${part("month")}-${part("day")}`;
}
export function formatDate(value:string,locale:Locale="tr"){
 const date=new Date(value);return Number.isNaN(date.getTime())?"—":new Intl.DateTimeFormat(tags[locale],{dateStyle:"medium",timeZone:"Europe/Istanbul"}).format(date);
}
export function formatDateTime(value:string,locale:Locale="tr"){
 const date=new Date(value);return Number.isNaN(date.getTime())?"—":new Intl.DateTimeFormat(tags[locale],{dateStyle:"medium",timeStyle:"short",timeZone:"Europe/Istanbul"}).format(date);
}
export function formatNumber(value:number,locale:Locale="tr"){return new Intl.NumberFormat(tags[locale],{maximumFractionDigits:2}).format(value);}
/** Aggregate meter quantities may exceed a single meter's index limit. */
export function formatConsumption(value:string,locale:Locale="tr"){
 if(!/^(?:0|[1-9][0-9]*)\.[0-9]{3}$/.test(value))throw new Error("Invalid consumption quantity");
 const [whole,fraction]=value.split('.');
 return new Intl.NumberFormat(tags[locale],{minimumFractionDigits:3,maximumFractionDigits:3}).formatToParts(BigInt(whole)).map(part=>part.type==='fraction'?fraction:part.value).join('');
}
export function formatCurrency(value:number,locale:Locale="tr",currency="TRY"){return new Intl.NumberFormat(tags[locale],{style:"currency",currency}).format(value);}
export function formatDecimalCurrency(value:string,locale:Locale="tr",currency="TRY"){
 if(!/^-?(?:0|[1-9][0-9]*)\.[0-9]{2}$/.test(value))throw new Error("Invalid decimal currency");
 const negative=value.startsWith("-");const [whole,fraction]=value.replace(/^-/,'').split('.');const units=BigInt(whole);
 const signedUnits=negative?(units===BigInt(0)?-0:-units):units;
 return new Intl.NumberFormat(tags[locale],{style:"currency",currency,minimumFractionDigits:2,maximumFractionDigits:2}).formatToParts(signedUnits).map(part=>part.type==='fraction'?fraction:part.value).join('');
}
export function formatPercent(value:number,locale:Locale="tr"){return new Intl.NumberFormat(tags[locale],{style:"percent",maximumFractionDigits:2}).format(value);}
