import test from 'node:test';
import assert from 'node:assert/strict';
import { validateApiUrl } from '../src/config/validate.ts';

test('unset API is allowed until backend integration', () => assert.equal(validateApiUrl('', false), null));
test('production rejects cleartext and unsafe protocols', () => {
  for (const url of ['http://api.example.com', 'file:///tmp/data', 'javascript:alert(1)']) assert.throws(() => validateApiUrl(url, false));
});
test('credentials, query and fragments cannot enter API configuration', () => {
  for (const url of ['https://user:pass@example.com', 'https://example.com?token=secret', 'https://example.com#secret']) assert.throws(() => validateApiUrl(url, true));
});
test('local development allows HTTP; HTTPS is normalized', () => {
  assert.equal(validateApiUrl('http://192.0.2.1:5005/', true), 'http://192.0.2.1:5005');
  assert.equal(validateApiUrl('https://api.example.com/', false), 'https://api.example.com');
});
