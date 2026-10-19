import {test} from 'node:test';
import assert from 'node:assert/strict';
import {formatPhone,phoneChars,validPhone} from '../src/profile/phone.ts';
test('Turkish numbers get +90 and 3-3-2-2 grouping whatever the user typed',()=>{
 for(const input of ['5321234567','05321234567','0532 123 45 67','905321234567','+905321234567','+90 (532) 123-45-67','00905321234567'])assert.equal(formatPhone(input),'+90 532 123 45 67',input);
 assert.equal(formatPhone('0212 555 66 77'),'+90 212 555 66 77');assert.equal(formatPhone(''),'');assert.equal(formatPhone('532'),'+90 532');
 assert.equal(validPhone('0532 123 45 67'),true);assert.equal(validPhone('0532 123 45'),false);assert.equal(validPhone('0632 123 45 67'),false);assert.equal(validPhone('+90 532 123 45 678'),false);
});
test('foreign numbers keep their own country code',()=>{
 assert.equal(formatPhone('+44 7911 123456'),'+447911123456');assert.equal(validPhone('+44 7911 123456'),true);assert.equal(validPhone('+4479'),false);assert.equal(formatPhone('0049301234567'),'+49301234567');
 assert.equal(phoneChars('a+90 5x32+1'),'+90 5321');
});
