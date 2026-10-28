export const qaCenterPath="/api/v1/qa-center";
export const qaCenterHref="/system-settings/qa-center";
export const qaCenterRoles=["SUPER_ADMIN"] as const;
export const qaRunStatuses=["QUEUED","RUNNING","PASSED","FAILED","CANCELLED","TIMED_OUT"] as const;
export const qaStepStatuses=["PASSED","FAILED","SKIPPED"] as const;
export const qaTabs=["overview","inventory","scenarios","assistant","runs","live","data","cleanup"] as const;
export const qaPlannedTabs=[["results","11"],["defects","12"],["settings","12"]] as const;
export type QaRunStatus=typeof qaRunStatuses[number];
export type QaStepStatus=typeof qaStepStatuses[number];
export type QaTab=typeof qaTabs[number];
export type QaScenario={key:string;timeoutSeconds:number;access:string};
export type QaRun={id:string;code:string;scenarioKey:string;status:QaRunStatus;attempt:number;retryOfId:string|null;cancelRequested:boolean;queuedAt:string;startedAt:string|null;finishedAt:string|null;resultCode:string|null;passedSteps:number;failedSteps:number;timeoutSeconds:number;version:number};
export type QaStep={sequence:number;stepKey:string;status:QaStepStatus;expected:string;actual:string;durationMs:number};
export type QaRunDetail={run:QaRun;steps:QaStep[]};
export type QaRunPage={items:QaRun[];total:number;page:number;pageSize:number};
export type QaOverview={environment:string;scenarioCount:number;endpointCount:number;activeRuns:number;runCounts:Record<QaRunStatus,number>;lastRun:QaRun|null};

type Value=Record<string,unknown>;
const object=(value:unknown):Value=>{if(!value||typeof value!=="object"||Array.isArray(value))throw new Error("Invalid QA response");return value as Value;};
const text=(value:unknown)=>{if(typeof value!=="string")throw new Error("Invalid QA response");return value;};
const optionalText=(value:unknown)=>value===null||value===undefined?null:text(value);
const count=(value:unknown)=>{if(typeof value!=="number"||!Number.isInteger(value)||value<0)throw new Error("Invalid QA response");return value;};
const member=<T extends string>(values:readonly T[],value:unknown):T=>{if(!values.includes(value as T))throw new Error("Invalid QA response");return value as T;};

export function readRun(value:unknown):QaRun{
 const v=object(value);
 return {id:text(v.id),code:text(v.code),scenarioKey:text(v.scenarioKey),status:member(qaRunStatuses,v.status),attempt:count(v.attempt),retryOfId:optionalText(v.retryOfId),
  cancelRequested:v.cancelRequested===true,queuedAt:text(v.queuedAt),startedAt:optionalText(v.startedAt),finishedAt:optionalText(v.finishedAt),resultCode:optionalText(v.resultCode),
  passedSteps:count(v.passedSteps),failedSteps:count(v.failedSteps),timeoutSeconds:count(v.timeoutSeconds),version:count(v.version)};
}
export function readDetail(value:unknown):QaRunDetail{
 const v=object(value);if(!Array.isArray(v.steps))throw new Error("Invalid QA response");
 return {run:readRun(v.run),steps:v.steps.map(item=>{const s=object(item);return {sequence:count(s.sequence),stepKey:text(s.stepKey),status:member(qaStepStatuses,s.status),expected:text(s.expected),actual:text(s.actual),durationMs:count(s.durationMs)};})};
}
export function readPage(value:unknown):QaRunPage{
 const v=object(value);if(!Array.isArray(v.items))throw new Error("Invalid QA response");
 return {items:v.items.map(readRun),total:count(v.total),page:count(v.page),pageSize:count(v.pageSize)};
}
export function readScenarios(value:unknown):QaScenario[]{
 if(!Array.isArray(value))throw new Error("Invalid QA response");
 return value.map(item=>{const v=object(item);return {key:text(v.key),timeoutSeconds:count(v.timeoutSeconds),access:text(v.access)};});
}
export function readOverview(value:unknown):QaOverview{
 const v=object(value),counts=object(v.runCounts);
 return {environment:text(v.environment),scenarioCount:count(v.scenarioCount),endpointCount:count(v.endpointCount),activeRuns:count(v.activeRuns),
  runCounts:Object.fromEntries(qaRunStatuses.map(status=>[status,count(counts[status]??0)])) as Record<QaRunStatus,number>,lastRun:v.lastRun?readRun(v.lastRun):null};
}
export type QaAiStatus={enabled:boolean;dailyLimit:number;usedToday:number};
export type QaAiPlan={scenarios:string[];summary:string;risks:string[];source:string;requiresApproval:boolean};
export type QaAiExplanation={runId:string;runStatus:QaRunStatus;explanation:string;source:string};
export const qaPromptLimit=1000;
const texts=(value:unknown)=>{if(!Array.isArray(value))throw new Error("Invalid QA response");return value.map(text);};
export function readAiStatus(value:unknown):QaAiStatus{const v=object(value);return {enabled:v.enabled===true,dailyLimit:count(v.dailyLimit),usedToday:count(v.usedToday)};}
export function readAiPlan(value:unknown):QaAiPlan{const v=object(value);if(v.requiresApproval!==true)throw new Error("Invalid QA response");return {scenarios:texts(v.scenarios),summary:text(v.summary),risks:texts(v.risks),source:text(v.source),requiresApproval:true};}
export function readAiExplanation(value:unknown):QaAiExplanation{const v=object(value);return {runId:text(v.runId),runStatus:member(qaRunStatuses,v.runStatus),explanation:text(v.explanation),source:text(v.source)};}
export const qaDataKinds=["DEMO","TEST"] as const;
export type QaDataKind=typeof qaDataKinds[number];
export type QaDataBatch={id:string;code:string;kind:QaDataKind;environment:string;runCode:string|null;status:"ACTIVE"|"CLEANED";recordCount:number;generatedAt:string;cleanedAt:string|null;propertyId:string|null;version:number};
export type QaCleanupPreview={batchId:string;code:string;kind:QaDataKind;canClean:boolean;items:{order:number;entityType:string;count:number}[];blockers:string[];confirmation:string;version:number};
export function readBatch(value:unknown):QaDataBatch{const v=object(value);return {id:text(v.id),code:text(v.code),kind:member(qaDataKinds,v.kind),environment:text(v.environment),runCode:optionalText(v.runCode),status:member(["ACTIVE","CLEANED"] as const,v.status),recordCount:count(v.recordCount),generatedAt:text(v.generatedAt),cleanedAt:optionalText(v.cleanedAt),propertyId:optionalText(v.propertyId),version:count(v.version)};}
export function readBatches(value:unknown):QaDataBatch[]{if(!Array.isArray(value))throw new Error("Invalid QA response");return value.map(readBatch);}
export function readPreview(value:unknown):QaCleanupPreview{const v=object(value);if(!Array.isArray(v.items))throw new Error("Invalid QA response");
 return {batchId:text(v.batchId),code:text(v.code),kind:member(qaDataKinds,v.kind),canClean:v.canClean===true,items:v.items.map(item=>{const i=object(item);return {order:count(i.order),entityType:text(i.entityType),count:count(i.count)};}),blockers:texts(v.blockers),confirmation:text(v.confirmation),version:count(v.version)};}
export const canConfirmCleanup=(preview:QaCleanupPreview,typed:string)=>preview.canClean&&typed===preview.confirmation;
export const isActive=(run:QaRun)=>run.status==="QUEUED"||run.status==="RUNNING";
export const canRetry=(run:QaRun)=>(run.status==="FAILED"||run.status==="CANCELLED"||run.status==="TIMED_OUT")&&run.attempt<5;
export const runsQuery=(page:number,status:QaRunStatus|"ALL")=>`${qaCenterPath}/runs?page=${page}&pageSize=10${status==="ALL"?"":`&status=${status}`}`;
export const scenarioMessageKey=(key:string)=>"scenario_"+key.replace(/[^a-z0-9]/gi,"_");
