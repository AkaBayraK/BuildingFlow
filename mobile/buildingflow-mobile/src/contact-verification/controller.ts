import {parseContact,parseOtpResult,phoneCommand,contactError,type Contact,type Outcome} from './contracts.ts';
const root='/api/v1/mobile/contact-verification';
export type State={contact:Contact|null;busy:boolean;error:string;outcome:Outcome|null;retryUntil:number};
type Ports={request:(path:string,body?:unknown)=>Promise<Response>;uuid:()=>string;active:()=>boolean;now:()=>number;verified:()=>void};
export class ContactController{
 private epoch=0;private sequence=0;private sending=false;
 private pending:{key:string;id:string;body:unknown}|null=null;
 private listeners=new Set<()=>void>();
 state:State={contact:null,busy:false,error:'',outcome:null,retryUntil:0};
 private ports:Ports;
 constructor(ports?:Ports){this.ports=ports??{request:async()=>{throw Error('Inactive');},uuid:()=>{throw Error('Inactive');},active:()=>false,now:Date.now,verified:()=>{}};}
 configure(ports:Ports){this.ports=ports;}
 subscribe=(listener:()=>void)=>{this.listeners.add(listener);return()=>{this.listeners.delete(listener);};};
 snapshot=()=>this.state;
 private publish(change:Partial<State>){this.state={...this.state,...change};this.listeners.forEach(l=>l());}
 private live(epoch:number){return epoch===this.epoch&&this.ports.active();}
 clear(){this.epoch++;this.sequence++;this.sending=false;this.pending=null;this.publish({contact:null,busy:false,error:'',outcome:null,retryUntil:0});}
 async refresh(){
  if(this.sending)return;if(!this.ports.active()){this.clear();return;}
  const epoch=this.epoch,ticket=++this.sequence;this.publish({busy:true});
  try{const r=await this.ports.request(root);if(!this.live(epoch)||ticket!==this.sequence)return;if(!r.ok)throw r.status;const c=parseContact(await r.json());if(!this.live(epoch)||ticket!==this.sequence)return;
   if(c.phone.status==='VERIFIED')this.pending=null;
   this.publish({contact:c,error:'',outcome:null,retryUntil:this.ports.now()+c.phone.retryAfterSeconds*1000});
  }catch(error){if(this.live(epoch)&&ticket===this.sequence)this.publish({contact:null,error:contactError(typeof error==='number'?error:0),outcome:null});}
  finally{if(this.live(epoch)&&ticket===this.sequence)this.publish({busy:false});}
 }
 async execute(action:'SEND'|'VERIFY'|'CANCEL'|'STAGE',value=''){
  const contact=this.state.contact;if(this.sending||!contact||!this.ports.active())return;
  if(action==='VERIFY'&&!/^[0-9]{6}$/.test(value)){this.publish({error:contactError(400)});return;}
  this.sending=true;this.sequence++;const epoch=this.epoch;this.publish({busy:true,error:'',outcome:null});
  const key=JSON.stringify([action,value]);const id=this.pending?.key===key?this.pending.id:this.ports.uuid();
  const body=this.pending?.id===id?this.pending.body:action==='STAGE'?{operationId:id,expectedVersion:contact.version,phone:value}:phoneCommand(contact,id,action,value);
  this.pending={key,id,body};
  try{
   const r=await this.ports.request(root+(action==='STAGE'?'/phone':'/phone/commands'),body);
   if(!this.live(epoch))return;
   if(!r.ok){if(r.status<500)this.pending=null;if(r.status===409){this.sending=false;await this.refresh();if(this.live(epoch))this.publish({error:contactError(409)});return;}throw r.status;}
   const raw:unknown=await r.json();if(!this.live(epoch))return;
   let c:Contact,outcome:Outcome|null=null,retry=0;
   if(action==='STAGE'){
    if(!raw||typeof raw!=='object'||!('operationId' in raw)||raw.operationId!==id||!('replayed' in raw)||typeof raw.replayed!=='boolean'||!('contact' in raw))throw Error('Invalid stage response');
    c=parseContact(raw.contact);
   }else{const result=parseOtpResult(raw,id);c=result.contact;outcome=result.outcome;retry=result.retryAfterSeconds;}
   this.pending=null;this.publish({contact:c,outcome,error:'',retryUntil:this.ports.now()+Math.max(retry,c.phone.retryAfterSeconds)*1000});
   if(outcome==='VERIFIED'||outcome==='ALREADY_VERIFIED')this.ports.verified();
  }catch(error){if(this.live(epoch))this.publish({error:contactError(typeof error==='number'?error:0)});}
  finally{if(epoch===this.epoch){this.sending=false;this.publish({busy:false});}}
 }
}
