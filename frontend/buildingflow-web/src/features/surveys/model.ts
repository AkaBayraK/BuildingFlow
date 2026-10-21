// Anketler: runtime checks for /api/v1/properties/{id}/surveys, form bodies, templates and the CSV export.
export const surveyCategories=["DUES","SERVICE","SECURITY","SOCIAL","PARKING","ENVIRONMENT","GENERAL"] as const;
export const surveyStates=["DRAFT","SCHEDULED","ACTIVE","EXPIRED","COMPLETED"] as const;
export const surveyAudiences=["ALL_RESIDENTS","OWNERS","TENANTS"] as const;
export const responseModes=["PER_UNIT","PER_ACCOUNT"] as const;
export const questionTypes=["SINGLE_CHOICE","MULTIPLE_CHOICE","RATING","TEXT"] as const;
export type SurveyCategory=typeof surveyCategories[number];
export type SurveyState=typeof surveyStates[number];
export type SurveyAudience=typeof surveyAudiences[number];
export type ResponseMode=typeof responseModes[number];
export type QuestionType=typeof questionTypes[number];
export const limits={title:200,description:2000,questions:30,question:500,option:200,minOptions:2,maxOptions:10} as const;
export type SurveyOption={id:string;position:number;text:string};
export type SurveyQuestion={id:string;position:number;text:string;type:QuestionType;isRequired:boolean;options:SurveyOption[]};
export type SurveySummary={id:string;propertyId:string;title:string;description:string|null;category:SurveyCategory;startDate:string;endDate:string;audience:SurveyAudience;responseMode:ResponseMode;isAnonymous:boolean;
 status:"DRAFT"|"PUBLISHED"|"CLOSED";state:SurveyState;responseCount:number;eligibleCount:number;questionCount:number;publishedAt:string|null;closedAt:string|null;version:number;createdAt:string;modifiedAt:string};
export type SurveyDetail={survey:SurveySummary;questions:SurveyQuestion[]};
export type SurveyCount<K extends string>={key:K;count:number};
export type SurveyPage={today:string;items:SurveySummary[];page:number;pageSize:number;totalCount:number;stateCounts:SurveyCount<SurveyState>[];categoryCounts:SurveyCount<SurveyCategory>[]};
export type SurveyOverview={today:string;total:number;createdThisMonth:number;drafts:number;active:number;completed:number;expired:number;responses:number;eligible:number;completedResponses:number;completedEligible:number;
 categories:SurveyCount<SurveyCategory>[];topParticipation:SurveySummary[];lastCompleted:SurveySummary|null};
export type QuestionResult={id:string;position:number;text:string;type:QuestionType;answered:number;options:{id:string;text:string;count:number}[];ratingAverage:number|null;ratingCounts:number[];texts:{text:string;unitNumber:string|null;submittedOn:string}[]};
export type SurveyResults={survey:SurveySummary;hidden:boolean;minAnonymousResults:number;questions:QuestionResult[]};

const obj=(v:unknown):v is Record<string,unknown>=>!!v&&typeof v==="object"&&!Array.isArray(v);
const guid=(v:unknown):v is string=>typeof v==="string"&&/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(v);
const int=(v:unknown,min=0)=>Number.isSafeInteger(v)&&Number(v)>=min;
const stamp=(v:unknown)=>typeof v==="string"&&Number.isFinite(Date.parse(v));
const text=(v:unknown,max:number):v is string=>typeof v==="string"&&!!v.trim()&&v.length<=max;
const one=<T extends string>(list:readonly T[],v:unknown):v is T=>list.includes(v as T);
export const calendarDay=(v:unknown):v is string=>typeof v==="string"&&/^\d{4}-\d{2}-\d{2}$/.test(v)&&new Date(v+"T00:00:00Z").toISOString().slice(0,10)===v;
const stateOf={DRAFT:["DRAFT"],PUBLISHED:["SCHEDULED","ACTIVE","EXPIRED"],CLOSED:["COMPLETED"]} as const;

export function readSummary(v:unknown,property:string):SurveySummary{
 if(!obj(v)||!guid(v.id)||v.propertyId!==property||!text(v.title,limits.title)||!(v.description===null||text(v.description,limits.description))||!one(surveyCategories,v.category)||!calendarDay(v.startDate)||!calendarDay(v.endDate)||v.endDate<v.startDate
  ||!one(surveyAudiences,v.audience)||!one(responseModes,v.responseMode)||typeof v.isAnonymous!=="boolean"||!one(["DRAFT","PUBLISHED","CLOSED"] as const,v.status)||!(stateOf[v.status] as readonly string[]).includes(v.state as string)
  ||!int(v.responseCount)||!int(v.eligibleCount)||!int(v.questionCount)||Number(v.questionCount)>limits.questions||!(v.publishedAt===null||stamp(v.publishedAt))||!(v.closedAt===null||stamp(v.closedAt))||!int(v.version,1)||!stamp(v.createdAt)||!stamp(v.modifiedAt))throw new Error("Invalid survey");
 if(v.status!=="DRAFT"&&Number(v.eligibleCount)<Number(v.responseCount))throw new Error("Invalid participation");
 return v as SurveySummary;
}
function readQuestion(v:unknown,index:number):SurveyQuestion{
 if(!obj(v)||!guid(v.id)||v.position!==index+1||!text(v.text,limits.question)||!one(questionTypes,v.type)||typeof v.isRequired!=="boolean"||!Array.isArray(v.options))throw new Error("Invalid question");
 const choice=v.type==="SINGLE_CHOICE"||v.type==="MULTIPLE_CHOICE";
 if(choice?v.options.length<limits.minOptions||v.options.length>limits.maxOptions:v.options.length!==0)throw new Error("Invalid options");
 v.options.forEach((o,i)=>{if(!obj(o)||!guid(o.id)||o.position!==i+1||!text(o.text,limits.option))throw new Error("Invalid option");});
 return v as SurveyQuestion;
}
export function readDetail(v:unknown,property:string):SurveyDetail{
 if(!obj(v)||!Array.isArray(v.questions))throw new Error("Invalid survey detail");
 const survey=readSummary(v.survey,property),questions=v.questions.map(readQuestion);
 if(questions.length!==survey.questionCount)throw new Error("Invalid question count");
 return {survey,questions};
}
function counts<K extends string>(v:unknown,keys:readonly K[]):SurveyCount<K>[]{
 if(!Array.isArray(v)||v.length!==keys.length||!v.every((c,i)=>obj(c)&&c.key===keys[i]&&int(c.count)))throw new Error("Invalid survey counts");
 return v as SurveyCount<K>[];
}
function readSummaries(v:unknown,property:string,max:number){if(!Array.isArray(v)||v.length>max)throw new Error("Invalid surveys");const items=v.map(r=>readSummary(r,property));if(new Set(items.map(i=>i.id)).size!==items.length)throw new Error("Duplicate surveys");return items;}
export function readPage(v:unknown,property:string):SurveyPage{
 if(!obj(v)||!calendarDay(v.today)||!int(v.page,1)||!int(v.pageSize,1)||Number(v.pageSize)>100||!int(v.totalCount))throw new Error("Invalid survey page");
 const items=readSummaries(v.items,property,Math.min(Number(v.pageSize),Number(v.totalCount)));
 return {today:v.today,items,page:v.page as number,pageSize:v.pageSize as number,totalCount:v.totalCount as number,stateCounts:counts(v.stateCounts,surveyStates),categoryCounts:counts(v.categoryCounts,surveyCategories)};
}
export function readOverview(v:unknown,property:string):SurveyOverview{
 if(!obj(v)||!calendarDay(v.today))throw new Error("Invalid overview");
 const numbers=["total","createdThisMonth","drafts","active","completed","expired","responses","eligible","completedResponses","completedEligible"] as const;
 if(!numbers.every(k=>int(v[k])))throw new Error("Invalid overview numbers");
 const categories=counts(v.categories,surveyCategories);if(categories.reduce((a,c)=>a+c.count,0)!==v.total)throw new Error("Invalid category total");
 return {...Object.fromEntries(numbers.map(k=>[k,v[k]])) as Record<typeof numbers[number],number>,today:v.today,categories,topParticipation:readSummaries(v.topParticipation,property,3),lastCompleted:v.lastCompleted===null?null:readSummary(v.lastCompleted,property)};
}
export function readResults(v:unknown,property:string):SurveyResults{
 if(!obj(v)||typeof v.hidden!=="boolean"||!int(v.minAnonymousResults,1)||!Array.isArray(v.questions))throw new Error("Invalid results");
 const survey=readSummary(v.survey,property);if(v.hidden&&v.questions.length)throw new Error("Hidden results carry data");
 const questions=v.questions.map((q,i)=>{
  if(!obj(q)||!guid(q.id)||q.position!==i+1||!text(q.text,limits.question)||!one(questionTypes,q.type)||!int(q.answered)||!Array.isArray(q.options)||!Array.isArray(q.ratingCounts)||!Array.isArray(q.texts))throw new Error("Invalid question result");
  if(!q.options.every(o=>obj(o)&&guid(o.id)&&text(o.text,limits.option)&&int(o.count))||!q.ratingCounts.every(c=>int(c))||!(q.ratingCounts.length===(q.type==="RATING"?5:0))
   ||!(q.ratingAverage===null||typeof q.ratingAverage==="number"&&q.ratingAverage>=1&&q.ratingAverage<=5)||!q.texts.every(t=>obj(t)&&text(t.text,1000)&&(t.unitNumber===null||typeof t.unitNumber==="string")&&calendarDay(t.submittedOn)))throw new Error("Invalid result values");
  if(survey.isAnonymous&&q.texts.some(t=>(t as {unitNumber:unknown}).unitNumber!==null))throw new Error("Anonymous result exposes a unit");
  return q as QuestionResult;});
 return {survey,hidden:v.hidden,minAnonymousResults:v.minAnonymousResults as number,questions};
}

/** Whole percent of eligible respondents who answered; 0 when nobody is eligible. */
export function participation(s:Pick<SurveySummary,"responseCount"|"eligibleCount">){return s.eligibleCount>0?Math.round(100*s.responseCount/s.eligibleCount):0;}
export function share(part:number,total:number){return total>0?Math.round(100*part/total):0;}

export type QuestionForm={key:string;text:string;type:QuestionType;isRequired:boolean;options:string[]};
export type SurveyForm={title:string;description:string;category:SurveyCategory;startDate:string;endDate:string;audience:SurveyAudience;responseMode:ResponseMode;isAnonymous:boolean;questions:QuestionForm[]};
export const isChoice=(type:QuestionType)=>type==="SINGLE_CHOICE"||type==="MULTIPLE_CHOICE";
export function addDays(day:string,days:number){const d=new Date(day+"T00:00:00Z");d.setUTCDate(d.getUTCDate()+days);return d.toISOString().slice(0,10);}
/** A question switched to/from a choice type keeps its text; options appear or disappear with the type. */
export function retype(q:QuestionForm,type:QuestionType):QuestionForm{return {...q,type,options:isChoice(type)?(q.options.length>=limits.minOptions?q.options:[...q.options,"",""].slice(0,Math.max(limits.minOptions,q.options.length))):[]};}
export function move<T>(list:readonly T[],from:number,to:number){if(to<0||to>=list.length)return [...list];const copy=[...list];const [item]=copy.splice(from,1);copy.splice(to,0,item);return copy;}
export type FormIssue="title"|"description"|"dates"|"questions"|`question:${number}`;
/** Client-side mirror of the server rules; the server stays the authority. Publishing additionally needs one question. */
export function surveyIssues(f:SurveyForm,publish=false):FormIssue[]{
 const issues:FormIssue[]=[];const title=f.title.trim();
 if(!title||title.length>limits.title)issues.push("title");
 if(f.description.trim().length>limits.description)issues.push("description");
 if(!calendarDay(f.startDate)||!calendarDay(f.endDate)||f.endDate<f.startDate||f.startDate<"2000-01-01"||f.endDate>"2100-12-31")issues.push("dates");
 if(f.questions.length>limits.questions||publish&&f.questions.length===0)issues.push("questions");
 f.questions.forEach((q,i)=>{const t=q.text.trim(),o=q.options.map(x=>x.trim());
  const bad=!t||t.length>limits.question||(isChoice(q.type)?o.length<limits.minOptions||o.length>limits.maxOptions||o.some(x=>!x||x.length>limits.option)||new Set(o.map(x=>x.toLocaleLowerCase("tr"))).size!==o.length:o.length>0);
  if(bad)issues.push(`question:${i}`);});
 return issues;
}
export function surveyBody(f:SurveyForm,version:number){
 return {title:f.title.trim(),description:f.description.trim()||null,category:f.category,startDate:f.startDate,endDate:f.endDate,audience:f.audience,responseMode:f.responseMode,isAnonymous:f.isAnonymous,
  questions:f.questions.map(q=>({text:q.text.trim(),type:q.type,isRequired:q.isRequired,options:isChoice(q.type)?q.options.map(o=>o.trim()):[]})),version};
}
let sequence=0;export const questionKey=()=>`q${++sequence}`;
export function formFrom(d:SurveyDetail):SurveyForm{
 const s=d.survey;return {title:s.title,description:s.description??"",category:s.category,startDate:s.startDate,endDate:s.endDate,audience:s.audience,responseMode:s.responseMode,isAnonymous:s.isAnonymous,
  questions:d.questions.map(q=>({key:questionKey(),text:q.text,type:q.type,isRequired:q.isRequired,options:q.options.map(o=>o.text)}))};
}

// Code-owned templates; every visible text is a TR/EN dictionary key under surveys.template_*.
export type SurveyTemplate={id:"satisfaction"|"dues"|"social"|"cleaning"|"security";category:SurveyCategory;days:number;anonymous:boolean;questions:{type:QuestionType;required:boolean;options:number}[]};
export const surveyTemplates:readonly SurveyTemplate[]=[
 {id:"satisfaction",category:"SERVICE",days:14,anonymous:true,questions:[{type:"RATING",required:true,options:0},{type:"SINGLE_CHOICE",required:true,options:4},{type:"TEXT",required:false,options:0}]},
 {id:"dues",category:"DUES",days:14,anonymous:false,questions:[{type:"SINGLE_CHOICE",required:true,options:3},{type:"MULTIPLE_CHOICE",required:false,options:4},{type:"TEXT",required:false,options:0}]},
 {id:"social",category:"SOCIAL",days:10,anonymous:false,questions:[{type:"MULTIPLE_CHOICE",required:true,options:5},{type:"SINGLE_CHOICE",required:true,options:3},{type:"TEXT",required:false,options:0}]},
 {id:"cleaning",category:"SERVICE",days:10,anonymous:true,questions:[{type:"RATING",required:true,options:0},{type:"MULTIPLE_CHOICE",required:false,options:4},{type:"TEXT",required:false,options:0}]},
 {id:"security",category:"SECURITY",days:14,anonymous:true,questions:[{type:"RATING",required:true,options:0},{type:"SINGLE_CHOICE",required:true,options:3},{type:"TEXT",required:false,options:0}]},
];
export function templateForm(tpl:SurveyTemplate,today:string,label:(key:string)=>string):SurveyForm{
 return {title:label(`template_${tpl.id}_title`),description:label(`template_${tpl.id}_description`),category:tpl.category,startDate:today,endDate:addDays(today,tpl.days),audience:"ALL_RESIDENTS",responseMode:"PER_UNIT",isAnonymous:tpl.anonymous,
  questions:tpl.questions.map((q,i)=>({key:questionKey(),text:label(`template_${tpl.id}_q${i+1}`),type:q.type,isRequired:q.required,options:Array.from({length:q.options},(_,o)=>label(`template_${tpl.id}_q${i+1}_o${o+1}`))}))};
}

/** CSV cell: quoted, with formula-looking text neutralised so a spreadsheet never executes resident input. */
export function csvCell(value:string|number){const s=String(value);const safe=/^[=+\-@\t\r]/.test(s)?"'"+s:s;return `"${safe.replace(/"/g,'""')}"`;}
export function resultsCsv(r:SurveyResults,label:{question:string;type:string;answer:string;count:string;unit:string;date:string;types:Record<QuestionType,string>;rating:(n:number)=>string}){
 const rows:(string|number)[][]=[[label.question,label.type,label.answer,label.count,label.unit,label.date]];
 for(const q of r.questions){
  if(isChoice(q.type))for(const o of q.options)rows.push([q.text,label.types[q.type],o.text,o.count,"",""]);
  else if(q.type==="RATING")q.ratingCounts.forEach((c,i)=>rows.push([q.text,label.types.RATING,label.rating(i+1),c,"",""]));
  else for(const t of q.texts)rows.push([q.text,label.types.TEXT,t.text,1,t.unitNumber??"",t.submittedOn]);
 }
 return "﻿"+rows.map(row=>row.map(csvCell).join(";")).join("\r\n")+"\r\n";
}
