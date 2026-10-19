import type {MarkupBlock,MarkupInline} from "./markup";

// Renders parsed blocks as React elements only; consent text is never injected as HTML.
function Inline({content}:{content:MarkupInline[]}){
 return <>{content.map((part,index)=>part.type==="bold"?<strong key={index}>{part.text}</strong>:part.type==="link"?<a key={index} href={part.href} target="_blank" rel="noopener noreferrer nofollow" className="text-primary underline underline-offset-2">{part.text}</a>:<span key={index} className="whitespace-pre-line">{part.text}</span>)}</>;
}

export function ConsentMarkupView({blocks,className=""}:{blocks:MarkupBlock[];className?:string}){
 return <div className={"space-y-3 break-words text-sm leading-6 "+className}>{blocks.map((block,index)=>{
  if(block.type==="heading"){const style=block.level===1?"text-lg font-semibold":block.level===2?"text-base font-semibold":"text-sm font-semibold";
   return block.level===1?<h3 key={index} className={style}><Inline content={block.content}/></h3>:block.level===2?<h4 key={index} className={style}><Inline content={block.content}/></h4>:<h5 key={index} className={style}><Inline content={block.content}/></h5>;}
  if(block.type==="list"){const items=block.items.map((item,i)=><li key={i}><Inline content={item}/></li>);
   return block.ordered?<ol key={index} start={block.start} className="list-decimal space-y-1 ps-6">{items}</ol>:<ul key={index} className="list-disc space-y-1 ps-6">{items}</ul>;}
  return <p key={index}><Inline content={block.content}/></p>;
 })}</div>;
}
