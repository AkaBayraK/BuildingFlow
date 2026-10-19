// User decision 17.10.2026 (same rule as the API's PersonNames): surname in capitals, each given name with only its first
// letter capitalised. Turkish i/İ and ı/I are mapped by hand so the result does not depend on the runtime's locale data.
export type Citizenship = "DOMESTIC" | "FOREIGN" | null | undefined;
const collapse = (value: string) => value.trim().split(/\s+/).filter(Boolean).join(" ");
const upper = (value: string, foreign: boolean) => (foreign ? value : value.replace(/i/g, "İ").replace(/ı/g, "I")).toUpperCase();
const lower = (value: string, foreign: boolean) => (foreign ? value : value.replace(/I/g, "ı").replace(/İ/g, "i")).toLowerCase();
export function givenName(value: string, citizenship?: Citizenship) {
  const foreign = citizenship === "FOREIGN";
  // A new word starts after a space or a hyphen (Ayşe-Nur).
  return lower(collapse(value), foreign).replace(/(^|[ -])(\p{L})/gu, (_, start: string, letter: string) => start + upper(letter, foreign));
}
export const familyName = (value: string, citizenship?: Citizenship) => upper(collapse(value), citizenship === "FOREIGN");
// Company titles (LEGAL) are kept as written.
export const personName = (first: string, last: string, kind?: string | null, citizenship?: Citizenship) =>
  kind === "LEGAL" ? { firstName: first.trim(), lastName: last.trim() } : { firstName: givenName(first, citizenship), lastName: familyName(last, citizenship) };
