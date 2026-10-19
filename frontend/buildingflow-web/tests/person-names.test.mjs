import {test} from 'node:test';
import assert from 'node:assert/strict';
import {familyName,givenName,personName} from '../src/lib/person-names.ts';
test('given names keep only the first letter capitalised with Turkish letters',()=>{
 for(const [input,expected] of [['abdulkadir','Abdulkadir'],['  ABDULKADİR   ismail ','Abdulkadir İsmail'],['IŞIL','Işıl'],['ayşe-nur','Ayşe-Nur'],['çağrı ömer','Çağrı Ömer']])assert.equal(givenName(input),expected);
});
test('family names are capitals; foreign citizens and company titles are handled separately',()=>{
 assert.equal(familyName('altunbayrak'),'ALTUNBAYRAK');assert.equal(familyName(' şimşek  yılmaz '),'ŞİMŞEK YILMAZ');assert.equal(familyName('ışık'),'IŞIK');
 assert.equal(givenName('WILLIAM','FOREIGN'),'William');assert.equal(familyName('smith','FOREIGN'),'SMITH');
 assert.deepEqual(personName(' Örnek Yapı A.Ş. ','','LEGAL','DOMESTIC'),{firstName:'Örnek Yapı A.Ş.',lastName:''});assert.deepEqual(personName('ali','veli','NATURAL','DOMESTIC'),{firstName:'Ali',lastName:'VELİ'});
});
