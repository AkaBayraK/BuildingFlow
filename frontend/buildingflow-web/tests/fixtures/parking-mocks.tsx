export const P="11111111-1111-4111-8111-111111111111";
export function useIdentity(){return {hasRealmRole:(role:string)=>role===(location.pathname==="/guard"?"SECURITY":"MANAGER")};}
export function usePropertyScope(){return {propertyId:P,property:{id:P,name:"Örnek Yönetim Alanı",type:"APARTMENT",isEnabled:true},loading:false,error:"",refresh:()=>{}};}
