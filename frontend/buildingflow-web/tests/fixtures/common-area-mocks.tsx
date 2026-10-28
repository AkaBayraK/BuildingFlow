import {P} from './common-area-api';
const readonly=()=>window.location.pathname==='/readonly';
export function useIdentity(){return{subject:readonly()?'fixture-resident':'fixture-manager',hasRealmRole:(role:string)=>readonly()?role==='RESIDENT':role==='MANAGER'};}
export function usePropertyScope(){return{propertyId:P,property:{id:P,type:'APARTMENT',name:'Örnek Yönetim Alanı',isEnabled:true},loading:false,error:'',refresh:()=>{}};}
