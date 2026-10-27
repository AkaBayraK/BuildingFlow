export type BackTarget={href:string;label:"backToProperty2"|"backToPeople"};

export function propertySubpageBack(path:string):BackTarget|null{
 const match=/^\/properties\/([^/]+)\/([^?#]+?)\/?$/.exec(path);if(!match)return null;
 const [,id,rest]=match;const parts=rest.split("/");
 if(parts[0]==="people"&&parts.length===2)return {href:`/properties/${id}/people`,label:"backToPeople"};
 if(parts.length!==1)return null;
 return {href:`/properties/${id}`,label:"backToProperty2"};
}
