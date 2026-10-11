const guid="[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}";
const workspacePath=new RegExp(`^(?:/units|/people|/dues|/payments|/expenses|/announcements|/documents|/meters|/reports|/properties(?:/${guid}(?:/people(?:/${guid})?|/units|/dues|/payments|/expenses|/announcements|/documents|/meters|/reports|/buildings(?:/${guid}(?:/units)?)?)?)?|/sites(?:/${guid}(?:/blocks/${guid})?)?)$`,"i");
// Exact local paths only; no query, fragment, protocol or arbitrary suffix.
export function isWorkspaceReturnPath(value:string){return workspacePath.test(value);}
