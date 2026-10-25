// Synthetic, loopback-only API. No real users, SMS, IdP or credentials.
const http=require('node:http'),fs=require('node:fs'),path=require('node:path'),crypto=require('node:crypto');
const web=path.resolve(__dirname,'../..'),bundle=process.argv[2]??'/tmp/bf-verify04-ui';
const real=process.argv.includes('--real');
const owner='77777777-7777-4777-8777-777777777777';let phone='+905551234567',pv=1,calls=0,login=null,mode='normal';
let state={version:0,email:{status:'VERIFIED',value:'fresh@example.test',available:true,checkedAt:new Date().toISOString()},phone:{status:'UNVERIFIED',profileValue:phone,verifiedValue:null,pendingValue:phone,verifiedAt:null,canSendCode:true,deliveryStatus:'NOT_REQUESTED',challengeId:null,expiresAt:null,attemptsRemaining:null,retryAfterSeconds:0}};
const receipts=new Map();const snapshot=()=>({...state,email:{...state.email,checkedAt:new Date().toISOString()},phone:{...state.phone}});
const server=http.createServer(async(req,res)=>{
 res.setHeader('Cache-Control','no-store');const url=new URL(req.url,'http://127.0.0.1:13047');const body=await new Promise(resolve=>{let s='';req.on('data',x=>s+=x);req.on('end',()=>resolve(s));});
 const json=(v,status=200)=>{res.writeHead(status,{'Content-Type':'application/json'});res.end(JSON.stringify(v));};
 if(url.pathname==='/bundle.js'){res.setHeader('Content-Type','text/javascript; charset=utf-8');return res.end(fs.readFileSync(path.join(bundle,'bundle.js')));}
 if(url.pathname==='/style.css'){res.setHeader('Content-Type','text/css');let css='';function walk(dir){if(!fs.existsSync(dir))return;for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,e.name);if(e.isDirectory())walk(p);else if(p.endsWith('.css'))css+=fs.readFileSync(p,'utf8');}}walk(path.join(web,'.next/static'));return res.end(css);}
 if(url.pathname==='/fixture-login'){login=JSON.parse(body);return json({ok:true});}
 if(url.pathname==='/fixture-state')return json({calls,mode,login,phoneStatus:state.phone.status,version:state.version});
 if(url.pathname==='/fixture-mode'){
  mode=url.searchParams.get('value')??'normal';
  if(mode==='external'){state.version++;state.phone={...state.phone,status:'VERIFIED',verifiedValue:phone,verifiedAt:new Date().toISOString(),pendingValue:null,deliveryStatus:'VERIFIED',canSendCode:false};}
  if(mode==='pending'){state.version++;state.phone={...state.phone,status:'PENDING',verifiedValue:'+905559876543',verifiedAt:new Date().toISOString(),pendingValue:phone,canSendCode:true,deliveryStatus:'NOT_REQUESTED',challengeId:null,expiresAt:null,retryAfterSeconds:0};}
  if(mode==='unavailable')state.phone.canSendCode=false;
  if(mode==='email-unverified')state.email.status='UNVERIFIED';
  if(mode==='expired')state.phone={...state.phone,deliveryStatus:'EXPIRED',expiresAt:new Date(Date.now()-1000).toISOString()};
  return json({mode});
 }
 if(real&&url.pathname.startsWith('/api/')){
  if(req.headers.authorization!=='Bearer fixture-only')return json({},401);
  const reply=await fetch('http://127.0.0.1:13048'+url.pathname+url.search,{method:req.method,headers:{Authorization:'Bearer fixture-only','Content-Type':'application/json'},...(body?{body}:{} )});
  res.writeHead(reply.status,{'Content-Type':'application/json; charset=utf-8'});return res.end(Buffer.from(await reply.arrayBuffer()));
 }
 if(url.pathname.startsWith('/api/')){
  if(req.headers.authorization!=='Bearer fixture-only')return json({},401);
  if(url.pathname==='/api/v1/account')return json({userId:owner,username:'fixture',email:'stale@example.test',roles:[],contexts:[],hasMore:false,profile:{userId:owner,firstName:'Ada',lastName:'DEMO',phone,photoId:null,photoSha256:null,version:pv,modifiedAt:null}});
  if(url.pathname==='/api/v1/account/consents')return json({userId:owner,items:[]});
  if(url.pathname==='/api/v1/account/profile/commands'){const c=JSON.parse(body);phone=c.phone;pv++;state.phone.profileValue=phone;return json({profile:{userId:owner,firstName:c.firstName,lastName:c.lastName,phone,photoId:null,photoSha256:null,version:pv,modifiedAt:null}});}
  if(url.pathname==='/api/v1/account/contact-verification'){if(mode==='outage')return json({},503);return json(snapshot());}
  if(url.pathname==='/api/v1/account/contact-verification/phone/commands'){
   const c=JSON.parse(body);calls++;
   if(mode==='http429')return json({code:'BF_RATE_LIMITED'},429);
   if(mode==='conflict'){mode='normal';state.version++;return json({code:'BF_MOBILE_SYNC_CONFLICT'},409);}
   if(receipts.has(c.operationId))return json({...receipts.get(c.operationId),replayed:true,contact:snapshot()});
   if(c.expectedVersion!==state.version)return json({code:'BF_MOBILE_SYNC_CONFLICT'},409);
   let outcome='UNAVAILABLE';
   if(c.action==='SEND'){await new Promise(resolve=>setTimeout(resolve,200));outcome=mode==='unknown'?'UNKNOWN':mode==='rate'?'RATE_LIMITED':'ACCEPTED';state.version++;state.phone={...state.phone,status:'PENDING',deliveryStatus:outcome==='RATE_LIMITED'?'NOT_REQUESTED':outcome,challengeId:crypto.randomUUID(),expiresAt:new Date(Date.now()+300000).toISOString(),attemptsRemaining:5,retryAfterSeconds:outcome==='RATE_LIMITED'?3600:0};}
   else if(c.action==='VERIFY'){outcome=c.code==='123456'?'VERIFIED':'REJECTED';state.version++;state.phone.attemptsRemaining--;if(outcome==='VERIFIED'){state.phone={...state.phone,status:'VERIFIED',verifiedValue:phone,verifiedAt:new Date().toISOString(),pendingValue:null,deliveryStatus:'VERIFIED',canSendCode:false};pv++;}}
   else if(c.action==='CANCEL'){outcome='CANCELLED';state.version++;state.phone.deliveryStatus='CANCELLED';}
   const result={operationId:c.operationId,replayed:false,outcome,contact:snapshot(),retryAfterSeconds:state.phone.retryAfterSeconds};receipts.set(c.operationId,result);return json(result);
  }
  return json({},404);
 }
 res.setHeader('Content-Type','text/html; charset=utf-8');res.end('<!doctype html><html lang="tr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>BF-VERIFY04 isolated acceptance</title><link rel="stylesheet" href="/style.css"></head><body><div id="root"></div><script src="/bundle.js"></script></body></html>');
});server.listen(13047,'127.0.0.1',()=>console.log('[BUILDINGFLOW] BF-VERIFY04 isolated fixture http://127.0.0.1:13047/account (no real SMS)'));
