// Native Node.js tests; application TypeScript is checked by the Next.js build.
import {test} from "node:test";
import assert from "node:assert/strict";
import {isWorkspaceReturnPath} from "../src/auth/return-path.ts";
import {formatDateOnly} from "../src/lib/format.ts";
const property="9b962f3e-8812-459f-a42f-45ec6e3c6eba";
const building="24d39d57-021b-4705-87d3-98a091c95f23";
test("login return allows people and existing canonical/legacy routes",()=>{
  for(const path of ["/properties","/sites",`/properties/${property}`,`/properties/${property}/people`,`/properties/${property}/buildings/${building}`,`/sites/${property}/blocks/${building}`])assert.equal(isWorkspaceReturnPath(path),true,path);
});
test("login return rejects external URLs, credentials, extra suffixes and malformed IDs",()=>{
  for(const path of ["https://example.test/", "//example.test/", "javascript:alert(1)",`/properties/${property}/people?token=secret`,`/properties/${property}/people#code=secret`,`/properties/${property}/people/extra`,"/properties/not-a-guid/people",`/properties/${property}/../admin`,`/properties/${property}/%2e%2e/admin`,`/properties/${property}/blocks/${building}`,`/sites/${property}/people`," /properties","/properties\n"])assert.equal(isWorkspaceReturnPath(path),false,path);
});
test("calendar dates retain the same day in Turkish and English",()=>{
  assert.equal(formatDateOnly("2026-10-08","tr"),"08.10.2026");
  assert.equal(formatDateOnly("2026-10-08","en"),"10/08/2026");
  assert.equal(formatDateOnly("not-a-date"),"—");
});
