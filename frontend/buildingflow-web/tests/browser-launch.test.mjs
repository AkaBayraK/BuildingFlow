import {test} from "node:test";
import assert from "node:assert/strict";
import {EventEmitter} from "node:events";
import vm from "node:vm";
import {isBuildingFlowTab, macBrowserScript, openDevelopmentBrowser} from "../scripts/browser-launch.mjs";

const origin="http://localhost:3003", authority="http://localhost:8080/realms/buildingflow", client="buildingflow-web";
const login=(redirect=origin+"/",clientId=client,base=authority)=>base+"/protocol/openid-connect/auth?"+new URLSearchParams({client_id:clientId,redirect_uri:redirect,state:"private-state",code_challenge:"private-challenge"});

function runner(replies) {
  const calls=[],messages=[];
  function spawn(command,args,options) {
    const call={command,args,options};calls.push(call);
    const child=new EventEmitter();child.stdout=new EventEmitter();child.stdin=new EventEmitter();
    child.stdin.end=value=>{call.input=value;};
    const reply=replies.shift();
    setImmediate(()=>{if(reply instanceof Error)child.emit("error",reply);else {if(reply?.output)child.stdout.emit("data",Buffer.from(reply.output));child.emit("close",reply?.code??0);}});
    return child;
  }
  return {calls,messages,options:{platform:"darwin",mode:undefined,spawn,info:value=>messages.push(value),warn:value=>messages.push(value)}};
}
function nativeRun(chrome,safari) {
  return vm.runInNewContext(macBrowserScript+"\nrun(args)",{args:[origin,authority,client],Application:id=>id==="com.google.Chrome"?chrome:safari});
}
const stopped={running:()=>false};

test("browser reuse matches only this frontend origin and its configured OIDC login",()=>{
  for(const url of [origin,origin+"/",origin+"/properties/x/dues",origin+"?view=1",origin+"#section",login(),login(origin+"/properties/x")])assert.equal(isBuildingFlowTab(url,origin,authority,client),true);
  for(const url of [origin+"0/",origin+".example/",origin+"@example.com/", "http://localhost:3010/", "https://localhost:3003/",login("http://localhost:3010/"),login(origin+".example/"),login(origin+"/","other-client"),login(origin+"/",client,"http://localhost:8080/realms/other"),null])assert.equal(isBuildingFlowTab(url,origin,authority,client),false);
});

test("malformed or duplicated OIDC parameters cannot select another login tab",()=>{
  for(const url of [login()+"&client_id=other",login()+"&redirect_uri="+encodeURIComponent(origin+"/"),authority+"/protocol/openid-connect/auth?client_id=buildingflow-web&redirect_uri=%invalid",authority+"/protocol/openid-connect/auth?client_id=buildingflow-web",login().replace("client_id=","other_client_id=")])assert.equal(isBuildingFlowTab(url,origin,authority,client),false);
});

test("native reuse focuses the matching Chrome window/tab without navigating the login",()=>{
  const addresses=["http://localhost:3010/",login()];const actions=[];
  const windows=[{tabs:()=>[{url:()=>addresses[0]}]},{tabs:()=>addresses.map(address=>({url:()=>address}))}];
  const chrome={running:()=>true,windows:()=>windows,activate:()=>actions.push("chrome")};
  assert.equal(nativeRun(chrome,stopped),"reused");assert.equal(windows[1].activeTabIndex,2);assert.equal(windows[1].index,1);assert.deepEqual(actions,["chrome"]);assert.equal(addresses[1],login());
});

test("native reuse handles Safari, no matching tab and unavailable automation",()=>{
  const tab={url:()=>origin+"/properties/x"};const window={tabs:()=>[tab]};let activated=false;
  const safari={running:()=>true,windows:()=>[window],activate:()=>{activated=true;}};
  assert.equal(nativeRun(stopped,safari),"reused");assert.equal(window.currentTab,tab);assert.equal(activated,true);
  assert.equal(nativeRun(stopped,stopped),"not-found");
  assert.equal(nativeRun({running:()=>true,windows:()=>{throw Error("denied");}},safari),"unavailable");
});

test("existing browser tab prevents any OS open call and does not log OIDC details",async()=>{
  const run=runner([{output:"reused\n"}]);assert.equal(await openDevelopmentBrowser(origin,run.options),"reused");
  assert.deepEqual(run.calls.map(call=>call.command),["osascript"]);assert.deepEqual(run.calls[0].args,["-l","JavaScript","-",origin,authority,client]);assert.equal(run.calls[0].input,macBrowserScript);assert.equal(run.calls[0].options.stdio[2],"ignore");
  assert.equal(run.messages.join(" ").includes("private-state"),false);
});

test("only a confirmed missing tab permits normal browser opening",async()=>{
  const run=runner([{output:"not-found\n"},{}]);assert.equal(await openDevelopmentBrowser(origin,run.options),"opened");assert.deepEqual(run.calls.map(call=>call.command),["osascript","open"]);assert.deepEqual(run.calls[1].args,[origin]);
});

test("denial, timeout, process error and unexpected output never open a duplicate",async()=>{
  for(const reply of [{output:"unavailable\n"},{code:1},{code:null,output:"not-found\n"},new Error("spawn failed"),{output:"private-state"}]) {
    // A signal termination has a null code, which the process adapter must reject.
    const run=runner([reply]);
    if(reply.code===null)run.options.spawn=(command,args,options)=>{run.calls.push({command,args,options});const c=new EventEmitter();c.stdout=new EventEmitter();c.stdin=new EventEmitter();c.stdin.end=()=>{};setImmediate(()=>c.emit("close",null));return c;};
    assert.equal(await openDevelopmentBrowser(origin,run.options),"unavailable");assert.equal(run.calls.length,1);assert.equal(run.calls[0].options.timeout,8000);assert.equal(run.messages.join(" ").includes("private-state"),false);
  }
});

test("disabled and explicit always modes plus Windows/Linux preserve launcher controls",async()=>{
  let run=runner([]);assert.equal(await openDevelopmentBrowser(origin,{...run.options,mode:"false"}),"disabled");assert.equal(run.calls.length,0);
  run=runner([{}]);assert.equal(await openDevelopmentBrowser(origin,{...run.options,mode:"always"}),"opened");assert.equal(run.calls[0].command,"open");
  for(const [platform,command,args] of [["win32","cmd.exe",["/c","start","",origin]],["linux","xdg-open",[origin]]]){run=runner([{}]);assert.equal(await openDevelopmentBrowser(origin,{...run.options,platform}),"opened");assert.equal(run.calls[0].command,command);assert.deepEqual(run.calls[0].args,args);}
  run=runner([new Error("opener failed")]);assert.equal(await openDevelopmentBrowser(origin,{...run.options,mode:"always"}),"unavailable");assert.equal(run.messages.length,1);
});
