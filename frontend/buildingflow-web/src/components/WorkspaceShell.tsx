"use client";
import Link from "next/link";
import {BuildingFlowMark} from "@/components/BuildingFlowMark";
import {useEffect,useRef,useState} from "react";
import {usePathname} from "next/navigation";
import {useTranslations} from "next-intl";
import {Building2,LayoutDashboard,DoorOpen,Users,FileText,ReceiptText,WalletCards,ChartNoAxesCombined,Menu,Search,ChevronDown,Plus,Car,BriefcaseBusiness,Wrench,Settings,SlidersHorizontal,MessageSquareText,UserCog,ShieldCheck,UserRound,ListChecks} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Dialog,DialogContent,DialogHeader,DialogTitle,DialogDescription} from "@/components/ui/dialog";
import {SessionControls,useIdentity} from "@/auth/AuthGate";
import {PropertyScopeProvider,usePropertyScope} from "@/features/navigation/PropertyScope";
import {managementSection} from "@/features/navigation/routes";
import {pickSlogan} from "@/features/dashboard/slogans";
export function WorkspaceShell({children,centralPeople=false,sessionContexts=false}:{children:React.ReactNode;centralPeople?:boolean;sessionContexts?:boolean}){
 const path=usePathname();
 if(sessionContexts&&(path==="/"||path.startsWith("/workspaces")))return <ContextShell>{children}</ContextShell>;
 return <PropertyScopeProvider><Shell centralPeople={centralPeople} sessionContexts={sessionContexts}>{children}</Shell></PropertyScopeProvider>;
}
function Shell({children,centralPeople,sessionContexts}:{children:React.ReactNode;centralPeople:boolean;sessionContexts:boolean}){
 const [menuOpen,setMenuOpen]=useState(false);const [collapsed,setCollapsed]=useState(false);const [searchOpen,setSearchOpen]=useState(false);const [query,setQuery]=useState("");const content=useRef<HTMLDivElement>(null);const [slogan,setSlogan]=useState(0);
 const t=useTranslations("ui"),w=useTranslations("sessionWorkspaces");const path=usePathname();const scope=usePropertyScope();const identity=useIdentity();const properties=path.startsWith("/properties");
 useEffect(()=>{content.current?.scrollTo({top:0,left:0});},[path]);
 // İŞ20: one slogan per page load, chosen after hydration so server and client markup match.
 useEffect(()=>{setSlogan(pickSlogan(Math.random()));},[]);
 const finance=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"].some(role=>identity.hasRealmRole(role));
 const platform=identity.hasRealmRole("SUPER_ADMIN");
 const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(role=>identity.hasRealmRole(role));
 const section=scope.propertyId?managementSection(path,scope.propertyId):path==="/units"?"units":(path==="/people"||path.startsWith("/people/"))?"people":path==="/dues"?"dues":path==="/payments"?"payments":path==="/expenses"?"expenses":path==="/announcements"?"announcements":path==="/documents"?"documents":path==="/meters"?"meters":path==="/reports"?"reports":path==="/website"?"website":path==="/requests"?"requests":path==="/sms"?"sms":path==="/users"?"users":path==="/account"?"account":path==="/dashboard"?"dashboard":path.startsWith("/consents")?"consents":null;
 const title=section==="website"?t("navWebsite"):section==="dashboard"?t("navDashboard"):section==="account"?t("navAccount"):section==="consents"?t("navConsents"):section==="requests"?t("shellRequests"):section==="users"?t("navUsers"):section==="sms"?t("navSms"):section==="meters"?t("navMeters"):section==="reports"?t("navReports"):section==="announcements"?t("navAnnouncements"):section==="documents"?t("navDocuments"):section==="expenses"?t("navExpenses"):section==="payments"?t("navPayments"):section==="dues"?t("navDues"):section==="units"?t("navUnits"):section==="buildings"?t("navBuildings"):section==="people"?t("people"):scope.propertyId?t("navOverview"):properties?t("properties"):t("overview");
 const base=scope.propertyId?`/properties/${scope.propertyId}`:null;
 const items=[...(base?[{section:"overview",href:base,label:t("navOverview"),Icon:FileText}]:[]),...(base && scope.property && scope.property.type!=="APARTMENT"?[{section:"buildings",href:base+"/buildings",label:t("navBuildings"),Icon:Building2}]:[]),{section:"units",href:base?base+"/units":"/units",label:scope.property?.type==="APARTMENT"?t("apartments"):t("navUnits"),Icon:DoorOpen},...(finance?[{section:"meters",href:base?base+"/meters":"/meters",label:t("navMeters"),Icon:ChartNoAxesCombined},{section:"reports",href:base?base+"/reports":"/reports",label:t("navReports"),Icon:ChartNoAxesCombined},{section:"dues",href:base?base+"/dues":"/dues",label:t("navDues"),Icon:ReceiptText},{section:"payments",href:base?base+"/payments":"/payments",label:t("navPayments"),Icon:WalletCards},{section:"expenses",href:base?base+"/expenses":"/expenses",label:t("navExpenses"),Icon:ChartNoAxesCombined}]:[]),{section:"announcements",href:base?base+"/announcements":"/announcements",label:t("navAnnouncements"),Icon:FileText},{section:"documents",href:base?base+"/documents":"/documents",label:t("navDocuments"),Icon:FileText},...(manager?[{section:"website",href:base?base+"/website":"/website",label:t("navWebsite"),Icon:Building2},{section:"requests",href:base?base+"/requests":"/requests",label:t("shellRequests"),Icon:Wrench},{section:"people",href:centralPeople?"/people":base?base+"/people":"/people",label:t("people"),Icon:Users},{section:"sms",href:base?base+"/sms":"/sms",label:t("navSms"),Icon:MessageSquareText}]:[]),...((platform||!sessionContexts&&identity.hasRealmRole("SITE_ADMIN"))?[{section:"users",href:"/users",label:t("navUsers"),Icon:UserCog}]:[])];
 const dashboard={href:"/dashboard",label:t("navDashboard"),Icon:LayoutDashboard,active:path==="/dashboard"};
 const home={href:sessionContexts?"/workspaces":"/",label:sessionContexts?w("title"):t("overview"),Icon:ListChecks,active:path==="/"};
 const propertyLink={href:"/properties",label:t("properties"),Icon:Building2,active:path==="/properties"};
 const shortcuts=[dashboard,propertyLink,...items.filter(item=>item.section==="units"||item.section==="people").map(item=>({...item,active:section===item.section}))];
 const searchable=[dashboard,propertyLink,...items.map(item=>({...item,active:section===item.section})),...(manager&&(!sessionContexts||platform)?[{href:"/consents",label:t("navConsents"),Icon:ShieldCheck,active:section==="consents"}]:[]),{href:"/account",label:t("navAccount"),Icon:UserRound,active:section==="account"},home];
 useEffect(()=>{function shortcut(event:KeyboardEvent){if((event.metaKey||event.ctrlKey)&&event.key.toLowerCase()==="k"){event.preventDefault();setSearchOpen(value=>!value);}}window.addEventListener("keydown",shortcut);return()=>window.removeEventListener("keydown",shortcut);},[]);
 const mainItems=items.filter(item=>["overview","buildings","units","people"].includes(item.section));
 const operationItems=items.filter(item=>!["overview","buildings","units","people"].includes(item.section));
 const planned=[{label:t("shellSharedAreas"),Icon:Plus},{label:t("shellParking"),Icon:Car},{label:t("shellSuppliers"),Icon:BriefcaseBusiness}];
 function links(values:typeof items){return values.map(item=><Link key={item.section} href={item.href} aria-label={item.label} title={item.label} className={section===item.section?"active":""} aria-current={section===item.section?"page":undefined} onNavigate={()=>setMenuOpen(false)}><item.Icon size={18} aria-hidden="true"/><span>{item.label}</span></Link>);}
 const navigation=<>
  <Link className="brand" href="/dashboard" onNavigate={()=>setMenuOpen(false)} aria-label={t("buildingflowOverview")}><BuildingFlowMark size={44} variant="light"/><span>Building<span className="brand-flow">Flow</span></span></Link>
  <div className="sidebar-scroll">
   <span className="label">{t("shellMainMenu")}</span>
   <nav aria-label={t("mainNavigation")}>{[dashboard,propertyLink].map(item=><Link key={item.href} href={item.href} aria-label={item.label} title={item.label} className={item.active?"active":""} aria-current={item.active?"page":undefined} onNavigate={()=>setMenuOpen(false)}><item.Icon size={18} aria-hidden="true"/><span>{item.label}</span></Link>)}{links(mainItems)}{planned.map(item=><button key={item.label} className="sidebar-planned" disabled title={t("shellPlanned")}><item.Icon size={18} aria-hidden="true"/><span>{item.label}</span></button>)}</nav>
   <span className="label property-menu-title" title={scope.property?.name}>{t("shellOperations")}</span>
   <nav aria-label={scope.propertyId?t("propertyMenu"):t("managementModules")}>{links(operationItems)}</nav>
   {manager&&(!sessionContexts||platform)&&<><span className="label">{t("shellAdministration")}</span><nav aria-label={t("shellAdministration")}><Link href="/consents" aria-label={t("navConsents")} title={t("navConsents")} className={section==="consents"?"active":""} aria-current={section==="consents"?"page":undefined} onNavigate={()=>setMenuOpen(false)}><ShieldCheck size={18} aria-hidden="true"/><span>{t("navConsents")}</span></Link>{[{label:t("shellDefinitions"),Icon:SlidersHorizontal},{label:t("shellUsers"),Icon:Users},{label:t("shellSettings"),Icon:Settings}].map(item=><button key={item.label} className="sidebar-planned" disabled title={t("shellPlanned")}><item.Icon size={18}/><span>{item.label}</span>{item.Icon===SlidersHorizontal&&<ChevronDown size={14}/>}</button>)}</nav></>}
   <nav className="sidebar-end" aria-label={home.label}><Link href={home.href} aria-label={home.label} title={home.label} className={home.active?"active":""} aria-current={home.active?"page":undefined} onNavigate={()=>setMenuOpen(false)}><home.Icon size={18} aria-hidden="true"/><span>{home.label}</span></Link></nav>
  </div>
 </>;
 return <div className={`shell${collapsed?" shell-collapsed":""}`}>
  <a className="workspace-skip" href="#workspace-content">{t("skipToContent")}</a>
  <aside className="workspace-sidebar">{navigation}</aside>
  <main className="workspace-main">
   <header className="workspace-header"><div className="workspace-heading"><Button variant="ghost" size="icon" className="workspace-menu-button" aria-label={t("openNavigation")} aria-expanded={menuOpen||!collapsed} onClick={()=>{if(window.matchMedia("(min-width:1024px)").matches)setCollapsed(value=>!value);else setMenuOpen(true);}}><Menu aria-hidden="true"/></Button><button className="workspace-search" onClick={()=>setSearchOpen(true)}><Search size={18} aria-hidden="true"/><span>{t("shellSearch")}</span><kbd>⌘ K</kbd></button><p className="workspace-slogan" aria-live="off">{slogan?t(`shellSlogan${slogan}` as `shellSlogan${1|2|3|4|5|6|7|8}`):""}</p><span className="sr-only">{title}</span></div><SessionControls/></header>
   <div id="workspace-content" className="workspace-content" ref={content} role="region" aria-label={t("pageContent")} tabIndex={0}>{children}</div>
   <footer className="workspace-footer"><nav className="workspace-bottom-nav" aria-label={t("bottomNavigation")}>{shortcuts.map(item=><Link key={item.href} href={item.href} aria-label={item.label} title={item.label} className={item.active?"active":""} aria-current={item.active?"page":undefined}><item.Icon size={18} aria-hidden="true"/><span>{item.label}</span></Link>)}</nav></footer>
  </main>
  <Dialog open={searchOpen} onOpenChange={setSearchOpen}><DialogContent><DialogHeader><DialogTitle>{t("shellSearchTitle")}</DialogTitle><DialogDescription>{t("shellSearchDescription")}</DialogDescription></DialogHeader><input className="shell-search-input" aria-label={t("shellSearchTitle")} value={query} onChange={event=>setQuery(event.target.value)}/><nav className="shell-search-results">{searchable.filter(item=>item.label.toLocaleLowerCase().includes(query.toLocaleLowerCase())).map(item=><Link key={item.href} href={item.href} onNavigate={()=>{setSearchOpen(false);setQuery("");}}><item.Icon size={18}/>{item.label}</Link>)}{!searchable.some(item=>item.label.toLocaleLowerCase().includes(query.toLocaleLowerCase()))&&<p>{t("shellSearchEmpty")}</p>}</nav></DialogContent></Dialog>
  <Dialog open={menuOpen} onOpenChange={setMenuOpen}><DialogContent placement="right" className="workspace-mobile-menu left-0 right-auto flex max-w-xs flex-col gap-0 p-4 sm:max-w-xs data-[state=closed]:slide-out-to-left data-[state=open]:slide-in-from-left"><DialogHeader className="sr-only"><DialogTitle>{t("mainNavigation")}</DialogTitle><DialogDescription>{t("managementModules")}</DialogDescription></DialogHeader>{navigation}</DialogContent></Dialog>
 </div>;
}

function ContextShell({children}:{children:React.ReactNode}){const w=useTranslations("sessionWorkspaces"),ui=useTranslations("ui");return <div className="min-h-screen bg-background text-foreground"><header className="flex flex-wrap items-center justify-between gap-4 border-b bg-card px-5 py-4"><Link href="/workspaces" className="flex items-center gap-3 font-semibold"><BuildingFlowMark size={36}/><span>BuildingFlow</span></Link><SessionControls/></header><nav aria-label={w("navigation")} className="flex gap-4 border-b px-5 py-3"><Link className="min-h-11 content-center text-primary underline" href="/workspaces">{w("title")}</Link><Link className="min-h-11 content-center text-primary underline" href="/account">{ui("navAccount")}</Link></nav><main className="px-5">{children}</main></div>;}
