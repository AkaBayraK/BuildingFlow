import {test} from "node:test";
import assert from "node:assert/strict";
import {readFileSync} from "node:fs";
import {parseOffsets,quiet,readCustomType,readDispatch,readOverview,readSource,readType,render,sampleValues,unknownVariables} from "../src/features/automations/model.ts";
import {errorCodes} from "../src/services/error-codes.ts";
import {isWorkspaceReturnPath} from "../src/auth/return-path.ts";
const property="9b962f3e-8812-459f-a42f-45ec6e3c6eba",id="24d39d57-021b-4705-87d3-98a091c95f23",corr="c89825a1-beb6-4850-a74d-0b59b5a77af3";
const tr=JSON.parse(readFileSync(new URL("../messages/tr.json",import.meta.url))),en=JSON.parse(readFileSync(new URL("../messages/en.json",import.meta.url)));
const health={outboxPending:0,outboxRetrying:0,smsQueued:1,smsFailedToday:0,executionsFailed:0,executionsDeadLetter:0,lastExecutionAt:null,emailChannelAvailable:false,smsAccountEnabled:true,pushAvailable:false};
const rule={customTypeId:null,customTypeName:null,icon:null,id,propertyId:property,type:"DUES_DUE_REMINDER",status:"ACTIVE",name:"Hatırlatma",startsOn:null,endsOn:null,sendHour:10,quietStartHour:21,quietEndHour:9,offsetDays:[-3,0],inApp:true,sms:true,email:false,titleTemplate:"t",bodyTemplate:"b",ruleVersion:2,activatedAt:"2026-11-06T10:00:00Z",nextRunAt:null,lastExecution:null,version:2};
const dispatch={id,definitionId:id,type:"DUES_DUE_REMINDER",channel:"SMS",status:"QUEUED",reasonCode:null,unitNumber:"22",period:"2026-01-01",offsetDays:-2,amount:1000,recipientMasked:"+90*******67",title:null,createdAt:"2026-11-06T10:00:00Z",correlationId:corr,ruleVersion:2,smsStatus:"QUEUED",smsAttempts:0,smsErrorCode:null};

test("overview reader keeps property scope and push stays unavailable",()=>{
 const value=readOverview({propertyId:property,today:"2026-11-06",types:[],definitions:[rule],activeCount:1,pausedCount:0,executionsToday:0,channels:[{channel:"IN_APP",delivered:1,queued:0,skipped:0,failed:0,cancelled:0}],health},property);
 assert.equal(value.definitions[0].offsetDays.length,2); assert.equal(value.health.pushAvailable,false);
 assert.throws(()=>readOverview({propertyId:id,today:"2026-11-06",types:[],definitions:[],activeCount:0,pausedCount:0,executionsToday:0,channels:[],health},property));
 assert.throws(()=>readOverview({propertyId:property,today:"2026-11-06",types:[],definitions:[{...rule,propertyId:id}],activeCount:0,pausedCount:0,executionsToday:0,channels:[],health},property));
});

test("dispatch reader refuses unmasked recipients and unknown channels",()=>{
 assert.equal(readDispatch(dispatch).status,"QUEUED");
 assert.throws(()=>readDispatch({...dispatch,recipientMasked:"+905551234567"}));
 assert.throws(()=>readDispatch({...dispatch,channel:"PUSH"}));
});

test("offset parsing, quiet window and template variables mirror the backend rules",()=>{
 assert.deepEqual(parseOffsets("0, -3,-2, -3"),[-3,-2,0]); assert.equal(parseOffsets("1,a"),null);
 assert.equal(quiet(21,9,22),true); assert.equal(quiet(21,9,10),false); assert.equal(quiet(0,0,3),false);
 assert.deepEqual(unknownVariables("{{balance}} {{fullName}}",["balance"]),["fullName"]);
 assert.equal(render("{{unitNumber}} no.lu: {{dueText}}",sampleValues),"22 no.lu: son ödemeye 2 gün kaldı");
});

test("automation route, error codes and dictionaries stay aligned",()=>{
 assert.equal(isWorkspaceReturnPath("/automations"),true); assert.equal(isWorkspaceReturnPath(`/properties/${property}/automations`),true);
 assert.equal(isWorkspaceReturnPath(`/properties/${property}/automations/x`),false);
 for(const view of ["types","history","executions","health"])assert.equal(isWorkspaceReturnPath(`/properties/${property}/automations/${view}`),true);
 for(const code of ["BF_AUTOMATION_INVALID","BF_AUTOMATION_CONFLICT","BF_AUTOMATION_QUIET_HOURS"]){assert.ok(errorCodes[code]);assert.ok(tr.apiErrors[code]);assert.ok(en.apiErrors[code]);}
 assert.deepEqual(Object.keys(tr.automations).sort(),Object.keys(en.automations).sort());
 const source=readFileSync(new URL("../src/features/automations/AutomationWorkspace.tsx",import.meta.url),"utf8")+readFileSync(new URL("../src/features/automations/AutomationTypes.tsx",import.meta.url),"utf8");
 for(const [,key] of source.matchAll(/\bt\("([A-Za-z_]+)"/g))assert.ok(key in tr.automations,key);
 for(const prefix of ["type_","channel_","dispatch_","exec_","trigger_","action_","status_","tab_"])assert.ok(Object.keys(tr.automations).some(k=>k.startsWith(prefix)),prefix);
 assert.equal(tr.ui.navAutomations,"Otomasyonlar");
});

const custom={id,propertyId:property,isPlatform:false,code:"custom-abc",name:"Asansör arızası",icon:"Wrench",sourceCode:"REQUEST",conditions:[{field:"category",operator:"IN",values:["ELEVATOR"]}],trigger:"EVENT",dateField:null,defaultOffsets:[],dayOfMonth:null,dayOfWeek:null,recipientScopes:["MANAGERS"],defaultTitle:"t",defaultBody:"b",status:"PUBLISHED",typeVersion:2,ruleCount:1,canEdit:true,version:2};
test("custom types stay inside the property or the platform and carry only code-owned building blocks",()=>{
 assert.equal(readCustomType(custom,property).sourceCode,"REQUEST");
 assert.equal(readCustomType({...custom,propertyId:null,isPlatform:true},property).isPlatform,true);
 assert.throws(()=>readCustomType({...custom,propertyId:corr},property));
 assert.throws(()=>readCustomType({...custom,isPlatform:true},property));
 assert.throws(()=>readCustomType({...custom,conditions:[{field:"category",operator:"LIKE",values:["x"]}]},property));
 assert.equal(readSource({code:"REQUEST",fields:[{name:"category",options:["ELEVATOR"]}],dateFields:["createdAt"],triggers:["EVENT"],recipientScopes:["MANAGERS"],variables:["unitNumber"]}).code,"REQUEST");
 assert.throws(()=>readSource({code:"REQUEST",fields:[],dateFields:[],triggers:["SQL"],recipientScopes:[],variables:[]}));
 const info=readType({type:"CUSTOM",defaultName:"x",defaultOffsets:[],minOffset:-60,maxOffset:60,variables:["unitNumber"],defaultTitle:"t",defaultBody:"b",smsTypeCode:"AUTOMATION_NOTICE",customTypeId:id,icon:"Wrench",trigger:"EVENT",sourceCode:"REQUEST"});
 assert.equal(info.customTypeId,id);
 assert.throws(()=>readType({type:"CUSTOM",defaultName:"x",defaultOffsets:[],minOffset:-60,maxOffset:60,variables:[],defaultTitle:"t",defaultBody:"b",smsTypeCode:"x"}));
 const keys=Object.keys(tr.automations);
 for(const k of ["source_REQUEST","source_SUPPLIER_CONTRACT","source_UNIT","source_MAINTENANCE_PLAN","source_METER","source_RESERVATION","scope_MANAGERS","scope_REQUESTER","option_ELEVATOR","option_CONFIRMED","weekday_7","triggerKind_CALENDAR","icon_Megaphone"])assert.ok(keys.includes(k),k);
});
