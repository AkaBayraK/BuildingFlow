import {test} from 'node:test';import assert from 'node:assert/strict';
import {readWeather,weatherCitySlug,weatherCondition} from '../src/features/dashboard/weather.ts';
const time='2026-10-21T12:00:00+03:00',row={city:'İstanbul',status:'FRESH',temperatureC:18.5,symbol:'partlycloudy_day',forecastAt:time,updatedAt:time,checkedAt:time,nextRefreshAt:time};
test('weather contract binds city, temperatures, timestamps and unavailable/stale states',()=>{
 assert.throws(()=>readWeather({...row,city:undefined},'unknown'));assert.equal(readWeather(row,'istanbul').temperatureC,18.5);assert.equal(readWeather({...row,status:'STALE'},'istanbul').status,'STALE');
 const unavailable={...row,status:'UNAVAILABLE',temperatureC:null,symbol:null,forecastAt:null,updatedAt:null,checkedAt:null};assert.equal(readWeather(unavailable,'istanbul').temperatureC,null);
 for(const patch of [{city:'Artvin'},{temperatureC:Infinity},{temperatureC:66},{updatedAt:'bad'},{status:'PRIVATE_EXCEPTION'},{symbol:'<script>'},{status:'UNAVAILABLE'}])assert.throws(()=>readWeather({...row,...patch},'istanbul'));
 assert.equal(weatherCitySlug('Istanbul'),'istanbul');assert.equal(weatherCitySlug('İzmir'),'izmir');assert.equal(weatherCitySlug('unknown'),null);
});
test('MET symbols map to controlled translated descriptions and existing icons',()=>{
 for(const [code,kind] of [['clearsky_night','clear'],['partlycloudy_day','partly'],['heavyrain','rain'],['lightsnowshowers_day','snow'],['rainandthunder','storm'],['fog','fog'],[null,'unknown'],['cloudy','cloudy']])assert.equal(weatherCondition(code),kind);
});
