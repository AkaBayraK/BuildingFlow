const moduleSelections=new Set(["units","people","dues","payments","expenses","announcements","surveys","documents","meters","maintenance","reports","sms","requests","website","common-areas","parking"]);

export type BackTarget={href:string;label:"back"|"backToPeople"};

export function propertySubpageBack(path:string):BackTarget|null{
 const match=/^\/properties\/([^/]+)\/([^?#]+?)\/?$/.exec(path);if(!match)return null;
 const [,id,rest]=match;const parts=rest.split("/");
 if(parts[0]==="people"&&parts.length===2)return {href:`/properties/${id}/people`,label:"backToPeople"};
 if(parts.length!==1)return null;
 return {href:moduleSelections.has(parts[0])?`/${parts[0]}`:`/properties/${id}`,label:"back"};
}
