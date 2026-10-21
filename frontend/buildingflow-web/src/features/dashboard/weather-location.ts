// Personal city is independent from management-area addresses. No GPS/IP lookup.
export const DEFAULT_WEATHER_CITY="İstanbul";
export function normalizeWeatherCity(value:unknown):string|null{
 if(typeof value!=="string")return null;
 const city=value.normalize("NFC").trim().replace(/\s+/g," ");
 return city.length>=2&&city.length<=60&&/^[\p{L}\p{M}]+(?:[ .’'-][\p{L}\p{M}]+)*$/u.test(city)?city:null;
}
const key=(subject:string)=>`buildingflow.weather-city.${subject}`;
export function readWeatherCity(storage:Pick<Storage,"getItem">,subject:string):string{
 try{return normalizeWeatherCity(storage.getItem(key(subject)))??DEFAULT_WEATHER_CITY;}catch{return DEFAULT_WEATHER_CITY;}
}
export function writeWeatherCity(storage:Pick<Storage,"setItem">,subject:string,city:string):boolean{
 const valid=normalizeWeatherCity(city);if(!valid)return false;
 try{storage.setItem(key(subject),valid);return true;}catch{return false;}
}
