"use client";
import {useCallback,useEffect,useRef,useState} from "react";
import Link from "next/link";
import {useLocale,useTranslations} from "next-intl";
import {ArrowUpRight,Bot,Building2,Gauge,Lock,MessageCircleQuestion,RefreshCw,SendHorizontal,Settings2,ShieldCheck,Sparkles,UserRound} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Skeleton} from "@/components/ui/skeleton";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {FilterChips} from "@/components/management/FilterChips";
import {ToneBadge,WorkspaceHeading} from "@/components/management/WorkspacePresentation";
import {formatDateOnly,formatDecimalCurrency,formatNumber} from "@/lib/format";
import {cn} from "@/lib/utils";
import {useDashboardArea} from "@/features/dashboard/DashboardArea";
import {assistantBadgeTone,assistantCategoryAll,assistantCategoryStyle,assistantKindIcon,assistantToolStyle,assistantWarningIcon} from "./assistant-style";
import {PromptManager} from "./PromptManager";
import {DraftPanel} from "./DraftPanel";
import {assistantCategories,assistantMessageLimit,assistantPath,assistantRoles,canAsk,chatBody,dimensionKey,promptText,promptTitle,readAnswer,readPrompts,readStatus,type AssistantAnswer,type AssistantCard,type AssistantCategory,type AssistantPrompt,type AssistantStatus} from "./model";

type Locale="tr"|"en";
type Turn={id:number;question:string;answer:AssistantAnswer|null;error:string};

export function AssistantPage(){const area=useDashboardArea();return <AssistantContent key={area.propertyId}/>;}

function AssistantContent(){
 const t=useTranslations("assistant"),ui=useTranslations("ui"),locale=useLocale() as Locale,api=useAuthorizedApi(),identity=useIdentity();
 const {allowed,properties,propertyId,listError,reload}=useDashboardArea();
 const [status,setStatus]=useState<AssistantStatus|null>(null),[prompts,setPrompts]=useState<AssistantPrompt[]|null>(null),[loadError,setLoadError]=useState(""),[revision,setRevision]=useState(0);
 const [category,setCategory]=useState<"ALL"|AssistantCategory>("ALL"),[message,setMessage]=useState(""),[turns,setTurns]=useState<Turn[]>([]),[busy,setBusy]=useState(false),[managing,setManaging]=useState(false);
 const lock=useRef(false),serial=useRef(0),end=useRef<HTMLDivElement>(null),manageButton=useRef<HTMLButtonElement>(null);
 const roleAllowed=allowed&&assistantRoles.some(r=>identity.hasRealmRole(r));
 const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(r=>identity.hasRealmRole(r));
 const property=properties?.find(p=>p.id===propertyId);

 useEffect(()=>{if(!roleAllowed)return;const c=new AbortController();setLoadError("");setStatus(null);setPrompts(null);
  void Promise.all([api<unknown>(`${assistantPath}/status`,{signal:c.signal}),api<unknown>(`${assistantPath}/prompts`,{signal:c.signal})])
   .then(([s,p])=>{if(!c.signal.aborted){setStatus(readStatus(s));setPrompts(readPrompts(p));}})
   .catch(e=>{if(!c.signal.aborted)setLoadError(errorMessage(e,locale));});
  return()=>c.abort();},[api,roleAllowed,revision,locale]);
 useEffect(()=>{end.current?.scrollIntoView({block:"nearest"});},[turns]);

 const ask=useCallback(async(input:{promptCode:string;label:string}|{message:string})=>{
  if(lock.current||!propertyId)return;lock.current=true;setBusy(true);
  const id=++serial.current;const question="label" in input?input.label:input.message.trim();
  setTurns(list=>[...list,{id,question,answer:null,error:""}]);
  try{
   const answer=readAnswer(await api<unknown>(`${assistantPath}/chat`,{method:"POST",body:JSON.stringify(chatBody(propertyId,locale,"label" in input?{promptCode:input.promptCode}:{message:input.message}))}),propertyId);
   setTurns(list=>list.map(turn=>turn.id===id?{...turn,answer}:turn));setStatus(current=>current?{...current,quota:answer.quota}:current);
   if(!("label" in input))setMessage("");
  }catch(e){setTurns(list=>list.map(turn=>turn.id===id?{...turn,error:errorMessage(e,locale)}:turn));}
  finally{lock.current=false;setBusy(false);}
 },[api,locale,propertyId]);

 const exhausted=status?.quota.remaining===0;
 const visible=(prompts??[]).filter(p=>category==="ALL"||p.category===category);
 const heading=<WorkspaceHeading title={t("title")} description={t("subtitle")} scope={property?.name??ui("selectProperty")} icon={Sparkles}
  action={<div className="flex flex-wrap gap-2"><Button variant="view" onClick={()=>setRevision(n=>n+1)} disabled={!status&&!loadError}><RefreshCw aria-hidden="true"/>{ui("refresh")}</Button>
   {status?.canManagePrompts&&<Button ref={manageButton} variant="edit" onClick={()=>setManaging(true)}><Settings2 aria-hidden="true"/>{t("managePrompts")}</Button>}</div>}/>;

 if(!roleAllowed)return <div className="py-8">{heading}<MessageBanner>{t("roleRequired")}</MessageBanner></div>;
 return <div className="assistant-page py-8">
  {heading}
  {listError?<MessageBanner action={<Button variant="view" size="sm" onClick={reload}><RefreshCw aria-hidden="true"/>{ui("tryAgain")}</Button>}>{listError}</MessageBanner>
  :properties&&properties.length===0?<MessageBanner>{t("noArea")}</MessageBanner>
  :loadError?<MessageBanner action={<Button variant="view" size="sm" onClick={()=>setRevision(n=>n+1)}><RefreshCw aria-hidden="true"/>{ui("tryAgain")}</Button>}>{loadError}</MessageBanner>
  :<>
   <div className="mb-4 flex flex-wrap gap-2" aria-label={t("scopeLabel")} role="group">
    <ToneBadge tone="success" icon={ShieldCheck}>{t("readOnly")}</ToneBadge>
    <ToneBadge tone="info" icon={Building2}>{t("scope",{name:property?.name??"—"})}</ToneBadge>
    <ToneBadge tone="neutral" icon={Lock}>{t("noPersonalData")}</ToneBadge>
    {status?<ToneBadge tone={status.mode==="AKAAI"?"violet":"teal"} icon={Bot}>{t(`mode_${status.mode}`)}</ToneBadge>:<Skeleton className="h-6 w-28 rounded-full"/>}
    {status&&<ToneBadge tone={exhausted?"error":"neutral"} icon={Gauge}>{t("quota",{used:status.quota.usedToday,limit:status.quota.dailyLimit})}</ToneBadge>}
   </div>
   <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,22rem)]">
    <section className="order-2 flex min-h-[28rem] min-w-0 flex-col rounded-2xl border bg-card lg:order-1" aria-labelledby="assistant-chat-title">
     <h2 id="assistant-chat-title" className="border-b px-4 py-3 font-semibold">{t("chatTitle")}</h2>
     <div className="flex-1 space-y-4 overflow-y-auto p-4" role="log" aria-live="polite" aria-relevant="additions" aria-busy={busy}>
      {turns.length===0&&<div className="grid place-items-center gap-2 py-10 text-center text-sm text-muted-foreground"><MessageCircleQuestion aria-hidden="true" className="size-8"/><p>{t("empty")}</p></div>}
      {turns.map(turn=><TurnView key={turn.id} turn={turn} locale={locale}/>)}
      <div ref={end}/>
     </div>
     <form className="border-t p-3" onSubmit={e=>{e.preventDefault();if(canAsk(message)&&!exhausted)void ask({message});}}>
      <label htmlFor="assistant-message" className="sr-only">{t("messageLabel")}</label>
      <div className="flex items-end gap-2">
       <textarea id="assistant-message" rows={2} maxLength={assistantMessageLimit} value={message} disabled={busy||!propertyId} onChange={e=>setMessage(e.target.value)} placeholder={t("placeholder")} aria-describedby="assistant-message-help"
        onKeyDown={e=>{if(e.key==="Enter"&&!e.shiftKey&&!e.nativeEvent.isComposing){e.preventDefault();e.currentTarget.form?.requestSubmit();}}}
        className="min-h-11 min-w-0 flex-1 resize-y rounded-lg border bg-background p-3 text-sm focus-visible:outline-2 focus-visible:outline-ring"/>
       <Button type="submit" size="icon" className="size-11 shrink-0" disabled={busy||!canAsk(message)||exhausted||!propertyId} aria-label={t("send")} title={t("send")}><SendHorizontal aria-hidden="true"/></Button>
      </div>
      <p id="assistant-message-help" className="mt-2 text-xs text-muted-foreground">{t(status?.mode==="RULES"?"helpRules":"helpModel",{count:message.trim().length,max:assistantMessageLimit})}</p>
     </form>
    </section>
    <aside className="order-1 min-w-0 space-y-3 rounded-2xl border bg-card p-4 lg:order-2" aria-labelledby="assistant-prompts-title">
     <h2 id="assistant-prompts-title" className="font-semibold">{t("promptsTitle")}</h2>
     <FilterChips label={t("categoryFilter")} value={category} onChange={value=>setCategory(value as "ALL"|AssistantCategory)} disabled={!prompts}
      items={[{value:"ALL",label:t("categoryAll"),icon:assistantCategoryAll},...assistantCategories.map(c=>({value:c,label:t(`category_${c}`),icon:assistantCategoryStyle[c].icon,tone:assistantCategoryStyle[c].tone}))]}/>
     {!prompts?<div className="space-y-2" aria-busy="true" aria-label={ui("loading")}>{Array.from({length:5},(_,i)=><Skeleton key={i} className="h-11 rounded-lg"/>)}</div>
     :visible.length===0?<p className="text-sm text-muted-foreground">{t("promptsEmpty")}</p>
     :<ul className="grid gap-2 sm:grid-cols-2 lg:grid-cols-1">{visible.map(p=>{const style=assistantToolStyle[p.toolKey];return <li key={p.id}>
      <button type="button" disabled={busy||exhausted||!propertyId} onClick={()=>void ask({promptCode:p.code,label:promptText(p,locale)})} title={promptText(p,locale)}
       className="flex min-h-11 w-full items-center gap-3 rounded-lg border bg-background px-3 py-2 text-left text-sm hover:bg-accent focus-visible:outline-2 focus-visible:outline-ring disabled:opacity-60">
       <span className={`request-type-icon tone-${style.tone}`}><style.icon aria-hidden="true"/></span><span className="min-w-0 flex-1">{promptTitle(p,locale)}</span></button></li>;})}</ul>}
     {exhausted&&<MessageBanner kind="warning" presentation="inline">{t("quotaReached")}</MessageBanner>}
     <p className="text-xs text-muted-foreground">{t("privacyNote")}</p>
     {manager&&propertyId&&<DraftPanel propertyId={propertyId} disabled={exhausted} onQuota={quota=>setStatus(current=>current?{...current,quota}:current)}/>}
    </aside>
   </div>
  </>}
  {managing&&<PromptManager trigger={manageButton.current} onClose={()=>setManaging(false)} onChanged={()=>setRevision(n=>n+1)}/>}
 </div>;
}

function TurnView({turn,locale}:{turn:Turn;locale:Locale}){
 const t=useTranslations("assistant");const Warning=assistantWarningIcon;
 return <article className="space-y-2">
  <div className="ml-auto flex max-w-[85%] items-start gap-2 rounded-2xl rounded-tr-sm bg-primary px-3 py-2 text-sm text-primary-foreground"><UserRound aria-hidden="true" className="mt-0.5 size-4 shrink-0"/><p className="min-w-0 whitespace-pre-line wrap-anywhere"><span className="sr-only">{t("you")}: </span>{turn.question}</p></div>
  {turn.error?<MessageBanner presentation="inline" announce={false}>{turn.error}</MessageBanner>
  :!turn.answer?<div className="flex max-w-[85%] items-center gap-2 rounded-2xl bg-muted px-3 py-2 text-sm text-muted-foreground" role="status"><Bot aria-hidden="true" className="size-4 animate-pulse"/>{t("thinking")}</div>
  :<div className="max-w-full space-y-3 rounded-2xl rounded-tl-sm border bg-muted/40 p-3 text-sm">
   <div className="flex flex-wrap items-center gap-2"><Bot aria-hidden="true" className="size-4"/><span className="sr-only">{t("assistant")}: </span>
    <ToneBadge tone={turn.answer.source==="AKAAI"?"violet":"teal"} icon={turn.answer.source==="AKAAI"?Sparkles:ShieldCheck}>{t(`source_${turn.answer.source}`)}</ToneBadge>
    {turn.answer.tool&&<ToneBadge tone={assistantBadgeTone[assistantToolStyle[turn.answer.tool].tone]} icon={assistantToolStyle[turn.answer.tool].icon}>{t(`tool_${turn.answer.tool}`)}</ToneBadge>}
    {turn.answer.modelFallback&&<ToneBadge tone="warning" icon={Warning}>{t("fallback")}</ToneBadge>}</div>
   <p className="whitespace-pre-line wrap-anywhere">{turn.answer.answer}</p>
   {turn.answer.sources.length>0&&<div><h3 className="mb-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">{t("sourcesTitle")}</h3>
    <ul className="grid gap-2 sm:grid-cols-2 xl:grid-cols-3">{turn.answer.sources.map((card,i)=><li key={`${card.key}-${card.dimension??""}-${i}`}><SourceCard card={card} locale={locale}/></li>)}</ul>
    <p className="mt-2 text-xs text-muted-foreground">{t("sourcesNote")}</p></div>}
  </div>}
 </article>;
}

function SourceCard({card,locale}:{card:AssistantCard;locale:Locale}){
 const t=useTranslations("assistant"),requests=useTranslations("requests"),expenses=useTranslations("expenses"),maintenance=useTranslations("maintenance");
 const Icon=assistantKindIcon[card.kind];const dimension=dimensionKey(card);
 const dimensionLabel=card.label??(dimension?(dimension.namespace==="requests"?requests:dimension.namespace==="expenses"?expenses:maintenance)(dimension.key as never):card.dimension&&!/^[A-Z]\d{1,3}$/.test(card.dimension)?card.dimension:null);
 const value=card.kind==="MONEY"?formatDecimalCurrency(card.value,locale):card.kind==="PERCENT"?`%${formatNumber(Number(card.value),locale)}`:card.kind==="DATE"?formatDateOnly(card.value,locale):card.kind==="DAYS"?t("days",{count:Number(card.value)}):card.kind==="SCORE"?t("score",{value:formatNumber(Number(card.value),locale)}):formatNumber(Number(card.value),locale);
 const range=card.from===card.to?formatDateOnly(card.from,locale):`${formatDateOnly(card.from,locale)} – ${formatDateOnly(card.to,locale)}`;
 return <div className="flex h-full items-start gap-2 rounded-xl border bg-card p-3">
  <span className={cn("request-type-icon",`tone-${assistantToolStyle[card.tool].tone}`)}><Icon aria-hidden="true"/></span>
  <div className="min-w-0 flex-1"><p className="text-base font-semibold tabular-nums">{value}</p><p className="text-xs">{t(`fact_${card.key}` as never)}{dimensionLabel&&<> · <span className="wrap-anywhere">{dimensionLabel}</span></>}</p><p className="text-xs text-muted-foreground">{range}</p></div>
  <Link href={card.href} className="grid size-11 shrink-0 place-items-center rounded-lg text-muted-foreground hover:bg-accent focus-visible:outline-2 focus-visible:outline-ring" aria-label={t("openScreen")} title={t("openScreen")}><ArrowUpRight aria-hidden="true" className="size-4"/></Link>
 </div>;
}
