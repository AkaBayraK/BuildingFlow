// Mirror of backend ConsentMarkup.Parse; docs/api/consent-markup-vectors.json pins web, mobile and API to the same result.
// Project-owned restricted format instead of HTML: nothing here is ever rendered as raw markup.
export type MarkupInline={type:"text";text:string}|{type:"bold";text:string}|{type:"link";text:string;href:string};
export type MarkupBlock={type:"heading";level:number;content:MarkupInline[]}|{type:"paragraph";content:MarkupInline[]}|{type:"list";ordered:boolean;start:number;items:MarkupInline[][]};
export type MarkupError="EMPTY"|"TOO_LONG"|"TOO_MANY_LINES"|"CONTROL_CHAR"|"LINK";
export type MarkupResult={blocks:MarkupBlock[];error:MarkupError|null};
export const markupLimits={maxLength:50_000,maxLines:2_000,maxHrefLength:2_000} as const;

const headingLine=/^(#{1,3}) ([^\n]*)$/,bulletLine=/^- ([^\n]*)$/,orderedLine=/^([0-9]{1,3})\. ([^\n]*)$/;
const email=/^[^\s@<>()[\]]+@[^\s@<>()[\]]+\.[^\s@<>()[\]]+$/;

export function normalizeMarkup(body:string){return body.replaceAll("\r\n","\n").replaceAll("\r","\n").replaceAll("\t"," ");}

// Control, invisible format (bidi overrides, BOM) and broken surrogate characters could make the approved text differ from what was shown.
export function hasHiddenCharacters(body:string){
 for(let i=0;i<body.length;i++){
  const code=body.charCodeAt(i);
  if(code>=0xd800&&code<=0xdbff){const next=body.charCodeAt(i+1);if(next>=0xdc00&&next<=0xdfff){const c=body.slice(i,i+2);i++;if(/\p{Cf}/u.test(c))return true;continue;}return true;}
  if(code>=0xdc00&&code<=0xdfff)return true;
  const c=body[i];if(c!=="\n"&&/\p{Cc}/u.test(c))return true;if(c!=="\u200c"&&c!=="\u200d"&&/\p{Cf}/u.test(c))return true;}
 return false;
}

export function safeHref(href:string){
 if(href.length===0||href.length>markupLimits.maxHrefLength)return false;
 if(href.startsWith("mailto:"))return email.test(href.slice(7));
 if(!href.startsWith("https://"))return false;
 try{const url=new URL(href);return url.protocol==="https:"&&url.hostname.length>0&&!url.username&&!url.password;}catch{return false;}
}

export function parseMarkup(input:string|null|undefined):MarkupResult{
 if(input==null)return {blocks:[],error:"EMPTY"};
 const body=normalizeMarkup(input);
 if(body.length>markupLimits.maxLength)return {blocks:[],error:"TOO_LONG"};
 if(hasHiddenCharacters(body))return {blocks:[],error:"CONTROL_CHAR"};
 const lines=body.split("\n");
 if(lines.length>markupLimits.maxLines)return {blocks:[],error:"TOO_MANY_LINES"};
 const blocks:MarkupBlock[]=[];const paragraph:string[]=[];let items:MarkupInline[][]|null=null,ordered=false,start=1;const state={error:null as MarkupError|null};
 const flushParagraph=()=>{if(!paragraph.length)return;const content=inline(paragraph.join("\n"),state);if(content.length)blocks.push({type:"paragraph",content});paragraph.length=0;};
 const flushList=()=>{if(!items)return;blocks.push({type:"list",ordered,start,items});items=null;};
 for(const raw of lines){
  const line=raw.trimEnd();
  if(line.trim().length===0){flushParagraph();flushList();continue;}
  const heading=headingLine.exec(line);
  if(heading&&heading[2].trim().length>0){flushParagraph();flushList();blocks.push({type:"heading",level:heading[1].length,content:inline(heading[2].trim(),state)});continue;}
  const bullet=bulletLine.exec(line),number=bullet?null:orderedLine.exec(line);
  const itemText=bullet?bullet[1]:number?number[2]:null;
  if(itemText!==null&&itemText.trim().length>0){
   flushParagraph();const isOrdered=!!number;
   if(items&&ordered!==isOrdered)flushList();
   if(!items){items=[];ordered=isOrdered;start=number?Number.parseInt(number[1],10):1;}
   items.push(inline(itemText.trim(),state));continue;
  }
  flushList();paragraph.push(line.trim());
 }
 flushParagraph();flushList();
 if(state.error)return {blocks:[],error:state.error};
 return blocks.length?{blocks,error:null}:{blocks:[],error:"EMPTY"};
}

function inline(text:string,state:{error:MarkupError|null}):MarkupInline[]{
 const result:MarkupInline[]=[];let plain="";
 const push=(value:MarkupInline)=>{if(plain){result.push({type:"text",text:plain});plain="";}result.push(value);};
 let i=0;
 while(i<text.length){
  if(text[i]==="*"&&text[i+1]==="*"){
   const close=text.indexOf("**",i+2);
   if(close>i+2){const inner=text.slice(i+2,close);if(inner.trim().length>0){push({type:"bold",text:inner});i=close+2;continue;}}
  }
  if(text[i]==="["){
   const closeText=text.indexOf("]",i+1);
   if(closeText>i+1&&closeText+1<text.length&&text[closeText+1]==="("&&!text.slice(i+1,closeText).includes("[")){
    const closeHref=text.indexOf(")",closeText+2),label=text.slice(i+1,closeText);
    if(closeHref>closeText+2&&!label.includes("\n")){
     const href=text.slice(closeText+2,closeHref);
     if(!/\s/.test(href)){
      if(!safeHref(href))state.error??="LINK";
      else if(label.trim().length>0){push({type:"link",text:label,href});i=closeHref+1;continue;}
     }
    }
   }
  }
  plain+=text[i];i++;
 }
 if(plain)result.push({type:"text",text:plain});
 return result;
}
