import {test} from 'node:test';
import assert from 'node:assert/strict';
import {familyName,givenName} from '../src/profile/names.ts';
test('name rule: given names capitalised per word, surname in capitals, Turkish letters kept',()=>{
 for(const [input,expected] of [['abdulkadir','Abdulkadir'],['  ABDULKADİR   ismail ','Abdulkadir İsmail'],['IŞIL','Işıl'],['ayşe-nur','Ayşe-Nur']])assert.equal(givenName(input),expected);
 assert.equal(familyName('altunbayrak'),'ALTUNBAYRAK');assert.equal(familyName('ışık şimşek'),'IŞIK ŞİMŞEK');assert.equal(givenName('WILLIAM','FOREIGN'),'William');
});
