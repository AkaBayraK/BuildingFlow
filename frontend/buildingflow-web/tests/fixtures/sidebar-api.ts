import {personCenterApi} from './person-center-api';
let firstName='Ada';
export function updateFixtureName(){firstName='Yeni';}
const subject='77777777-7777-4777-8777-777777777777';
const banners=new Map<string,{blob:Blob;record:Record<string,unknown>}>();
const photoAccounts=[['11111111-1111-4111-8111-111111111111','sistem.yoneticisi','Sistem','Yöneticisi','SUPER_ADMIN'],['22222222-2222-4222-8222-222222222222','yonetici','Site','Yöneticisi','SITE_ADMIN'],['33333333-3333-4333-8333-333333333333','rizasiz.yonetici','Örnek','Yönetici','MANAGER']].map(([id,username,firstName,lastName,role])=>({id,username,email:null,firstName,lastName,enabled:true,emailVerified:false,createdAt:null,roles:[role],propertyCount:1,linkedPeople:[],canEdit:true,canManagePhoto:true,photoId:null as string|null}));
const userPhotos=new Map<string,{blob:Blob;id:string;sha:string;version:number}>();
async function api(path:string,options:RequestInit={}){
  console.info("[fixture-request]",options.method??"GET",path);
  if(path.startsWith('/api/v1/users?'))return {items:photoAccounts,page:1,pageSize:20,totalCount:photoAccounts.length};
  const userPhoto=path.match(/^\/api\/v1\/users\/([^/]+)\/photo(?:\/(state|commands))?$/);
  if(userPhoto){const[,target,operation]=userPhoto;const entry=photoAccounts.find(u=>u.id===target)!;const saved=userPhotos.get(target);const canReplace=target!==photoAccounts[2].id;
   if(operation==='state')return {userId:target,photoId:saved?.id??null,photoSha256:saved?.sha??null,version:saved?.version??0,canReplace};
   if(operation==='commands'){const command=JSON.parse(String(options.body));if(command.photoAction==='REPLACE'){const bytes=Uint8Array.from(atob(command.photoBase64),c=>c.charCodeAt(0));const blob=new Blob([bytes],{type:'image/jpeg'});const sha=Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',bytes)),n=>n.toString(16).padStart(2,'0')).join('');const id=crypto.randomUUID();userPhotos.set(target,{blob,id,sha,version:(saved?.version??0)+1});entry.photoId=id;}else{userPhotos.delete(target);entry.photoId=null;}const current=userPhotos.get(target);return {operationId:command.operationId,replayed:false,photo:{userId:target,photoId:current?.id??null,photoSha256:current?.sha??null,version:current?.version??(saved?.version??0)+1,canReplace}};}
   return saved?.blob;
  }
  if(path.startsWith('/api/v1/weather?')){const city=new URLSearchParams(path.split('?')[1]).get('city');const names:Record<string,string>={istanbul:'İstanbul',izmir:'İzmir',ankara:'Ankara',artvin:'Artvin'};const now=new Date().toISOString();return {city:names[city??'istanbul'],status:city==='artvin'?'UNAVAILABLE':city==='ankara'?'STALE':'FRESH',temperatureC:city==='artvin'?null:city==='izmir'?24:18,symbol:city==='artvin'?null:'partlycloudy_day',forecastAt:city==='artvin'?null:now,updatedAt:city==='artvin'?null:now,checkedAt:city==='artvin'?null:now,nextRefreshAt:new Date(Date.now()+1800000).toISOString()};}
  if(path.includes('/dues/unit-choices?')){const q=new URLSearchParams(path.split('?')[1]),page=Number(q.get('page')??1),pageSize=Number(q.get('pageSize')??10),search=q.get('search')??'';const all=Array.from({length:48},(_,i)=>({id:`aaaaaaaa-aaaa-4aaa-8aaa-${String(i+1).padStart(12,'0')}`,buildingId:null,buildingName:null,number:String(i+1),isEnabled:true})).filter(u=>u.number.includes(search));return {items:all.slice((page-1)*pageSize,page*pageSize),page,pageSize,totalCount:all.length};}
  if(path.startsWith("/api/v1/properties?"))return {items:[area(subject,"Altunbayrak Rezidans",3,96),area(Q,"İkinci Yönetim Alanı",2,48)],page:1,pageSize:100,totalCount:2};
  if(path.endsWith("/dashboard")){const id=path.split("/")[4];await new Promise(r=>setTimeout(r,id===subject?250:700));return dashboard(id);}
  if(path.endsWith('/documents')&&options.method==='POST'){const id=path.split('/')[4],form=options.body as FormData,blob=form.get('file') as File;const sha=Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',await blob.arrayBuffer())),n=>n.toString(16).padStart(2,'0')).join('').toUpperCase();const record={id:'99999999-9999-4999-8999-999999999999',propertyId:id,requestId:form.get('requestId'),announcementId:null,audience:'ALL_MEMBERS',fileName:blob.name,contentType:blob.type,size:blob.size,sha256:sha,version:1,createdAt:new Date().toISOString(),replayed:false};banners.set(id,{blob,record});return record;}
  if(path.includes('/documents?')){const saved=banners.get(path.split('/')[4]);return {items:saved?[saved.record]:[],page:1,pageSize:100,totalCount:saved?1:0};}
  if(path.endsWith('/download'))return banners.get(path.split('/')[4])?.blob;

  if(path==='/api/v1/account/consents')return {userId:subject,items:[]};
  if(path==='/api/v1/account')return {userId:subject,username:'fixture',email:window.location.pathname==='/account'?'profil@example.test':null,roles:['SITE_ADMIN','ACCOUNTANT'],contexts:[],hasMore:false,profile:{userId:subject,firstName,lastName:'DEMO',phone:window.location.pathname==='/account'?'+90 555 123 45 67':null,photoId:null,photoSha256:null,version:1,modifiedAt:null}};
  if(path==='/api/v1/status')return {status:'fixture'};
  if(path.startsWith('/api/v2/'))return personCenterApi(path,options);
  throw new Error('Unexpected fixture API path');
}
export function useAuthorizedApi(){return api;}
export function errorMessage(){return 'İzole fixture API hatası';}

const Q='88888888-8888-4888-8888-888888888888';
function area(id:string,name:string,buildingCount:number,unitCount:number){return {id,name,city:'İstanbul',district:'Kadıköy',address:'Fixture',version:1,createdAt:'2026-10-20T10:00:00Z',isEnabled:true,type:'RESIDENCE',floorCount:null,buildingCount,unitCount,canChangeType:false};}
function dashboard(id:string){const n=id===Q?48:96;return {propertyId:id,today:'2026-10-20',currency:'TRY',unitCount:n,occupiedUnitCount:n-10,vacantUnitCount:10,receiptsThisMonth:'125400.00',overdueAmount:'12450.00',overdueChargeCount:12,upcomingAmount:'18400.00',upcomingChargeCount:8,requests:{recorded:5,inProgress:3},trend:['2026-05','2026-06','2026-07','2026-08','2026-09','2026-10'].map(period=>({period,receipts:period==='2026-10'?'125400.00':'80000.00',expenses:'45000.00'})),unitTypes:['APARTMENT','OFFICE','SHOP','VILLA','WAREHOUSE','WORKSHOP'].map((type,i)=>({type,count:i===0?n:0})),upcomingDues:[],maintenanceAvailable:false,collections:[{period:"2026-09",totalAmount:"100000.00",collectedAmount:"90000.00",pendingAmount:"0.00",overdueAmount:"10000.00"},{period:"2026-10",totalAmount:"158400.00",collectedAmount:"123600.00",pendingAmount:"22400.00",overdueAmount:"12400.00"}]};}
export const unitTypeNames={APARTMENT:'apartment',OFFICE:'office',SHOP:'shop',VILLA:'villa',WAREHOUSE:'warehouse',WORKSHOP:'workshop'};
