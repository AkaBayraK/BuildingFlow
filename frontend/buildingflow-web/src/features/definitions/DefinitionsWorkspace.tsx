"use client";
import {useCallback,useEffect,useMemo,useState} from "react";
import Link from "next/link";
import {useLocale,useTranslations} from "next-intl";
import {ExternalLink,FileText,Lock,RefreshCw,SlidersHorizontal} from "lucide-react";
import {useIdentity} from "@/auth/AuthGate";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {Button} from "@/components/ui/button";
import {Skeleton} from "@/components/ui/skeleton";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {FilterChips} from "@/components/management/FilterChips";
import {ToneBadge,WorkspaceHeading,type Tone} from "@/components/management/WorkspacePresentation";
import {allIcon,catalogIcon,fallbackStyle,groupStyle,levelStyle,listIcon,valueStyle} from "./definition-style";
import {definitionGroups,definitionLabelKey,definitionLevels,definitionRoles,definitionsPath,filterLists,readDefinitions,type DefinitionGroup,type DefinitionLevel,type DefinitionList,type Definitions} from "./model";

const panel="min-w-0 rounded-2xl border bg-card shadow-sm";
const levelTone:Record<DefinitionLevel,Tone>={FIXED:"neutral",SYSTEM:"info",PROPERTY:"success"};

// Yönetim > Tanımlar (TNM 01): mevcut parametrik listelerin salt okunur görünümü; düzenleme TNM 02/03 ile gelir.
export function DefinitionsWorkspace(){
 const t=useTranslations("definitions"),locale=useLocale(),api=useAuthorizedApi(),identity=useIdentity();
 const allowed=definitionRoles.some(role=>identity.hasRealmRole(role));
 const [data,setData]=useState<Definitions|null>(null),[loading,setLoading]=useState(true),[error,setError]=useState("");
 const [group,setGroup]=useState<DefinitionGroup|"ALL">("ALL"),[level,setLevel]=useState<DefinitionLevel|"ALL">("ALL");

 const load=useCallback(async(signal?:AbortSignal)=>{
  setLoading(true);
  try{const value=readDefinitions(await api<unknown>(definitionsPath,{signal}));if(!signal?.aborted){setData(value);setError("");}}
  catch(e){if(!signal?.aborted)setError(errorMessage(e,locale));}
  finally{if(!signal?.aborted)setLoading(false);}
 },[api,locale]);
 useEffect(()=>{if(!allowed)return;const c=new AbortController();void load(c.signal);return()=>c.abort();},[allowed,load]);

 const lists=useMemo(()=>filterLists(data?.lists??[],group,level),[data,group,level]);
 const catalogs=useMemo(()=>(data?.catalogs??[]).filter(c=>group==="ALL"||c.group===group),[data,group]);
 if(!allowed)return <div className="py-10"><MessageBanner>{t("roleRequired")}</MessageBanner></div>;

 const groupChips=[{value:"ALL",label:t("allGroups"),icon:allIcon,count:data?.lists.length},...definitionGroups.map(g=>({value:g,label:t(`group_${g}`),icon:groupStyle[g].icon,tone:groupStyle[g].tone,count:data?.lists.filter(l=>l.group===g).length}))];
 const levelChips=[{value:"ALL",label:t("allLevels"),icon:allIcon},...definitionLevels.map(l=>({value:l,label:t(`level_${l}`),icon:levelStyle[l].icon,tone:levelStyle[l].tone}))];

 return <div className="flex min-w-0 flex-col gap-4 py-8">
  <WorkspaceHeading title={t("title")} description={t("subtitle")} scope={t("scope")} icon={SlidersHorizontal} action={<Button variant="view" onClick={()=>void load()} disabled={loading}><RefreshCw className={loading?"animate-spin":undefined}/>{t("refresh")}</Button>}/>
  <MessageBanner kind="info">{t("readOnlyNotice")}</MessageBanner>
  {error&&<MessageBanner>{error}</MessageBanner>}
  <section className={panel+" flex flex-col gap-3 p-4"} aria-label={t("filters")}>
   <FilterChips label={t("groupFilter")} items={groupChips} value={group} onChange={value=>setGroup(value as DefinitionGroup|"ALL")}/>
   <FilterChips label={t("levelFilter")} items={levelChips} value={level} onChange={value=>setLevel(value as DefinitionLevel|"ALL")}/>
  </section>
  {loading&&!data?<div className="grid gap-4 lg:grid-cols-2">{Array.from({length:4},(_,i)=><Skeleton key={i} className="h-40 rounded-2xl"/>)}</div>:<>
   {group==="DOCUMENT"&&<div className={panel+" flex items-start gap-3 p-4"}><span className="request-type-icon tone-neutral"><FileText aria-hidden="true"/></span><div><h2 className="font-semibold">{t("group_DOCUMENT")}</h2><p className="text-sm text-muted-foreground">{t("documentTypesMissing")}</p></div></div>}
   {lists.length===0&&group!=="DOCUMENT"&&<p className="py-6 text-center text-sm text-muted-foreground">{t("noLists")}</p>}
   <div className="grid gap-4 lg:grid-cols-2">{lists.map(list=><DefinitionCard key={list.key} list={list}/>)}</div>
   {catalogs.length>0&&<section className={panel+" p-4"} aria-labelledby="definition-catalogs">
    <h2 id="definition-catalogs" className="font-semibold">{t("catalogsTitle")}</h2>
    <p className="mb-3 text-sm text-muted-foreground">{t("catalogsDescription")}</p>
    <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">{catalogs.map(c=>{const Icon=catalogIcon[c.key];return <div key={c.key} className="flex min-w-0 flex-col gap-2 rounded-xl border p-3">
     <div className="flex items-center gap-2 font-medium"><span className="request-type-icon tone-blue"><Icon aria-hidden="true"/></span>{t(`catalog_${c.key}`)}</div>
     <p className="text-sm text-muted-foreground">{t(`catalogNote_${c.key}`)}</p>
     <div className="mt-auto flex flex-wrap items-center gap-2"><ToneBadge tone="neutral" icon={groupStyle[c.group].icon}>{t(`scope_${c.scope}`)}</ToneBadge>
      {c.route?<Button asChild variant="view" size="sm"><Link href={c.route}><ExternalLink aria-hidden="true"/>{t("openCatalog")}</Link></Button>:<ToneBadge tone="warning" icon={Lock}>{t("catalogPlanned")}</ToneBadge>}</div>
    </div>;})}</div>
   </section>}
  </>}
 </div>;
}

function DefinitionCard({list}:{list:DefinitionList}){
 const t=useTranslations("definitions"),root=useTranslations();
 const Icon=listIcon[list.key],style=groupStyle[list.group];
 const label=(value:string)=>{const {namespace,key}=definitionLabelKey(list.key,value);const path=`${namespace}.${key}`;return root.has(path)?root(path):value;};
 return <section className={panel+" flex flex-col gap-3 p-4"} aria-labelledby={`definition-${list.key}`}>
  <div className="flex flex-wrap items-start justify-between gap-2">
   <div className="flex min-w-0 items-center gap-2"><span className={`request-type-icon tone-${style.tone}`}><Icon aria-hidden="true"/></span><div className="min-w-0"><h2 id={`definition-${list.key}`} className="font-semibold">{t(`list_${list.key}`)}</h2><p className="text-xs text-muted-foreground">{t(`group_${list.group}`)} · {t("valueCount",{count:list.values.length})}</p></div></div>
   <div className="flex flex-wrap gap-1.5"><ToneBadge tone={levelTone[list.level]} icon={levelStyle[list.level].icon}>{t(`level_${list.level}`)}</ToneBadge>{list.plannedPhase&&<ToneBadge tone="violet">{t("plannedPhase",{phase:list.plannedPhase.replace("_"," ")})}</ToneBadge>}</div>
  </div>
  <p className="text-sm text-muted-foreground">{t(`levelHint_${list.level}`)}</p>
  <ul className="flex flex-wrap gap-2" aria-label={t("valuesOf",{list:t(`list_${list.key}`)})}>{list.values.map(value=>{const s=valueStyle[list.key][value]??fallbackStyle;const V=s.icon;return <li key={value} className="request-type rounded-lg border px-2 py-1 text-sm" title={value}><span className={`request-type-icon tone-${s.tone}`}><V aria-hidden="true"/></span>{label(value)}</li>;})}</ul>
 </section>;
}
