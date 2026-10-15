const guid="[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}";
const workspacePath=new RegExp(`^(?:/units|/people|/dues|/payments|/expenses|/announcements|/documents|/meters|/reports|/properties(?:/${guid}(?:/people(?:/${guid})?|/units|/dues|/payments|/expenses|/announcements|/documents|/meters|/reports|/buildings(?:/${guid}(?:/units)?)?)?)?|/sites(?:/${guid}(?:/blocks/${guid})?)?)$`,"i");
// Allow one validated UUID filter on canonical unit lists; never retain auth query data.
const unitFilterPath=new RegExp(`^/properties/${guid}(?:/buildings/${guid})?/units\\?unitId=(${guid})$`,"i");
export function isWorkspaceReturnPath(value:string){const match=unitFilterPath.exec(value);return workspacePath.test(value)||(!!match&&match[1]!=="00000000-0000-0000-0000-000000000000");}
