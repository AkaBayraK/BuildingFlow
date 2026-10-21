export const weatherCities=[{id:"istanbul",name:"İstanbul"},{id:"ankara",name:"Ankara"},{id:"izmir",name:"İzmir"},{id:"artvin",name:"Artvin"}] as const;
export function weatherCitySlug(name:string){const folded=name.normalize("NFC").replaceAll("İ","I").toLowerCase();return weatherCities.find(c=>c.name.replaceAll("İ","I").toLowerCase()===folded)?.id??null;}
export type WeatherSnapshot={city:string;status:"FRESH"|"STALE"|"UNAVAILABLE";temperatureC:number|null;symbol:string|null;forecastAt:string|null;updatedAt:string|null;checkedAt:string|null;nextRefreshAt:string};
const date=(v:unknown):v is string=>typeof v==="string"&&/^\d{4}-\d{2}-\d{2}T.*(?:Z|[+-]\d{2}:\d{2})$/.test(v)&&Number.isFinite(Date.parse(v));
export function readWeather(value:unknown,slug:string):WeatherSnapshot{
 if(!value||typeof value!=="object"||Array.isArray(value))throw Error("Invalid weather");const v=value as Record<string,unknown>;
 const expected=weatherCities.find(c=>c.id===slug);
 if(!expected||v.city!==expected.name||!["FRESH","STALE","UNAVAILABLE"].includes(String(v.status))||!date(v.nextRefreshAt))throw Error("Invalid weather header");
 if(v.status==="UNAVAILABLE"){if([v.temperatureC,v.symbol,v.forecastAt,v.updatedAt,v.checkedAt].some(x=>x!==null))throw Error("Invalid unavailable weather");}
 else if(typeof v.temperatureC!=="number"||!Number.isFinite(v.temperatureC)||v.temperatureC< -100||v.temperatureC>65||![v.forecastAt,v.updatedAt,v.checkedAt].every(date)||!(v.symbol===null||typeof v.symbol==="string"&&/^[a-z_]{1,60}$/.test(v.symbol)))throw Error("Invalid weather data");
 return v as WeatherSnapshot;
}
export function weatherCondition(symbol:string|null){if(!symbol)return "unknown";if(symbol.includes("thunder"))return "storm";if(symbol.includes("snow")||symbol.includes("sleet"))return "snow";if(symbol.includes("rain"))return "rain";if(symbol.includes("fog"))return "fog";if(symbol.startsWith("clearsky"))return "clear";if(symbol.startsWith("fair")||symbol.startsWith("partlycloudy"))return "partly";if(symbol.startsWith("cloudy"))return "cloudy";return "unknown";}
