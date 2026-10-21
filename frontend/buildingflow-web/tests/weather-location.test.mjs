import {test} from 'node:test';
import assert from 'node:assert/strict';
import {DEFAULT_WEATHER_CITY,normalizeWeatherCity,readWeatherCity,writeWeatherCity} from '../src/features/dashboard/weather-location.ts';
test('personal weather city defaults to Istanbul and isolates preferences by account, never uses property city',()=>{
 const entries=new Map(),storage={getItem:k=>entries.get(k)??null,setItem:(k,v)=>entries.set(k,v)};
 assert.equal(DEFAULT_WEATHER_CITY,'İstanbul');assert.equal(readWeatherCity(storage,'one'),'İstanbul');
 assert.equal(writeWeatherCity(storage,'one','  İzmir  '),true);assert.equal(readWeatherCity(storage,'one'),'İzmir');assert.equal(readWeatherCity(storage,'two'),'İstanbul');
 const broken={getItem(){throw Error('Blocked')},setItem(){throw Error('Blocked')}};assert.equal(readWeatherCity(broken,'one'),'İstanbul');assert.equal(writeWeatherCity(broken,'one','Ankara'),false);
});
test('city preferences reject corrupt storage and markup, preserve Unicode city names',()=>{
 assert.equal(normalizeWeatherCity('  New   York '),'New York');assert.equal(normalizeWeatherCity('Şanlıurfa'),'Şanlıurfa');
 for(const value of [null,42,'','A','<script>x</script>','A'.repeat(61),'123'])assert.equal(normalizeWeatherCity(value),null);
 assert.equal(readWeatherCity({getItem:()=>'<script>x</script>'},'one'),'İstanbul');
});
