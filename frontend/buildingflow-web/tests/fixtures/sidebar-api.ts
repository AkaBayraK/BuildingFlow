let firstName='Ada';
export function updateFixtureName(){firstName='Yeni';}
const subject='77777777-7777-4777-8777-777777777777';
const banners=new Map<string,{blob:Blob;record:Record<string,unknown>}>();
async function api(path:string,options:RequestInit={}){
  console.info("[fixture-request]",options.method??"GET",path);
  if(path.startsWith("/api/v1/properties?"))return {items:[area(subject,"Altunbayrak Rezidans",3,96),area(Q,"İkinci Yönetim Alanı",2,48)],page:1,pageSize:100,totalCount:2};
  if(path.endsWith("/dashboard")){const id=path.split("/")[4];await new Promise(r=>setTimeout(r,id===subject?250:700));return dashboard(id);}
  if(path.endsWith('/documents')&&options.method==='POST'){const id=path.split('/')[4],form=options.body as FormData,blob=form.get('file') as File;const sha=Array.from(new Uint8Array(await crypto.subtle.digest('SHA-256',await blob.arrayBuffer())),n=>n.toString(16).padStart(2,'0')).join('').toUpperCase();const record={id:'99999999-9999-4999-8999-999999999999',propertyId:id,requestId:form.get('requestId'),announcementId:null,audience:'ALL_MEMBERS',fileName:blob.name,contentType:blob.type,size:blob.size,sha256:sha,version:1,createdAt:new Date().toISOString(),replayed:false};banners.set(id,{blob,record});return record;}
  if(path.includes('/documents?')){const saved=banners.get(path.split('/')[4]);return {items:saved?[saved.record]:[],page:1,pageSize:100,totalCount:saved?1:0};}
  if(path.endsWith('/download'))return banners.get(path.split('/')[4])?.blob;

  if(path==='/api/v1/account')return {userId:subject,username:'fixture',email:null,roles:['SITE_ADMIN','ACCOUNTANT'],contexts:[],hasMore:false,profile:{userId:subject,firstName,lastName:'DEMO',phone:null,photoId:null,photoSha256:null,version:1,modifiedAt:null}};
  if(path==='/api/v1/status')return {status:'fixture'};
  throw new Error('Unexpected fixture API path');
}
export function useAuthorizedApi(){return api;}
export function errorMessage(){return 'İzole fixture API hatası';}

const Q='88888888-8888-4888-8888-888888888888';
function area(id:string,name:string,buildingCount:number,unitCount:number){return {id,name,city:'İstanbul',district:'Kadıköy',address:'Fixture',version:1,createdAt:'2026-10-20T10:00:00Z',isEnabled:true,type:'RESIDENCE',floorCount:null,buildingCount,unitCount,canChangeType:false};}
function dashboard(id:string){const n=id===Q?48:96;return {propertyId:id,today:'2026-10-20',currency:'TRY',unitCount:n,occupiedUnitCount:n-10,vacantUnitCount:10,receiptsThisMonth:'125400.00',overdueAmount:'12450.00',overdueChargeCount:12,upcomingAmount:'18400.00',upcomingChargeCount:8,requests:{recorded:5,inProgress:3},trend:['2026-05','2026-06','2026-07','2026-08','2026-09','2026-10'].map(period=>({period,receipts:period==='2026-10'?'125400.00':'80000.00',expenses:'45000.00'})),unitTypes:['APARTMENT','OFFICE','SHOP','VILLA','WAREHOUSE','WORKSHOP'].map((type,i)=>({type,count:i===0?n:0})),upcomingDues:[],maintenanceAvailable:false};}
export const unitTypeNames={APARTMENT:'apartment',OFFICE:'office',SHOP:'shop',VILLA:'villa',WAREHOUSE:'warehouse',WORKSHOP:'workshop'};
