"use client";
import Link from "next/link";
import {BuildingFlowMark} from "@/components/BuildingFlowMark";
import {useEffect,useRef,useState} from "react";
import {usePathname} from "next/navigation";
import {useTranslations} from "next-intl";
import {Building2,LayoutDashboard,DoorOpen,Users,FileText,ReceiptText,WalletCards,ChartNoAxesCombined,Menu,Search,ChevronDown,Plus,Car,BriefcaseBusiness,Wrench,Settings,SlidersHorizontal} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Dialog,DialogContent,DialogHeader,DialogTitle,DialogDescription} from "@/components/ui/dialog";
import {SessionControls,useIdentity} from "@/auth/AuthGate";
import {PropertyScopeProvider,usePropertyScope} from "@/features/navigation/PropertyScope";
import {managementSection} from "@/features/navigation/routes";
export function WorkspaceShell({children}:{children:React.ReactNode}){
 return <PropertyScopeProvider><Shell>{children}</Shell></PropertyScopeProvider>;
}
function Shell({children}:{children:React.ReactNode}){
 const [menuOpen,setMenuOpen]=useState(false);const [collapsed,setCollapsed]=useState(false);const [searchOpen,setSearchOpen]=useState(false);const [query,setQuery]=useState("");const content=useRef<HTMLDivElement>(null);
 const t=useTranslations("ui");const path=usePathname();const scope=usePropertyScope();const identity=useIdentity();const properties=path.startsWith("/properties");
 useEffect(()=>{content.current?.scrollTo({top:0,left:0});},[path]);
 const finance=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"].some(role=>identity.hasRealmRole(role));
 const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(role=>identity.hasRealmRole(role));
 const section=scope.propertyId?managementSection(path,scope.propertyId):path==="/units"?"units":path==="/people"?"people":path==="/dues"?"dues":path==="/payments"?"payments":path==="/expenses"?"expenses":path==="/announcements"?"announcements":path==="/documents"?"documents":path==="/meters"?"meters":path==="/reports"?"reports":null;
 const title=section==="meters"?t("navMeters"):section==="reports"?t("navReports"):section==="announcements"?t("navAnnouncements"):section==="documents"?t("navDocuments"):section==="expenses"?t("navExpenses"):section==="payments"?t("navPayments"):section==="dues"?t("navDues"):section==="units"?t("navUnits"):section==="buildings"?t("navBuildings"):section==="people"?t("people"):scope.propertyId?t("navOverview"):properties?t("properties"):t("overview");
 const base=scope.propertyId?`/properties/${scope.propertyId}`:null;
 const items=[...(base?[{section:"overview",href:base,label:t("navOverview"),Icon:FileText}]:[]),...(base && scope.property && scope.property.type!=="APARTMENT"?[{section:"buildings",href:base+"/buildings",label:t("navBuildings"),Icon:Building2}]:[]),{section:"units",href:base?base+"/units":"/units",label:scope.property?.type==="APARTMENT"?t("apartments"):t("navUnits"),Icon:DoorOpen},...(finance?[{section:"meters",href:base?base+"/meters":"/meters",label:t("navMeters"),Icon:ChartNoAxesCombined},{section:"reports",href:base?base+"/reports":"/reports",label:t("navReports"),Icon:ChartNoAxesCombined},{section:"dues",href:base?base+"/dues":"/dues",label:t("navDues"),Icon:ReceiptText},{section:"payments",href:base?base+"/payments":"/payments",label:t("navPayments"),Icon:WalletCards},{section:"expenses",href:base?base+"/expenses":"/expenses",label:t("navExpenses"),Icon:ChartNoAxesCombined}]:[]),{section:"announcements",href:base?base+"/announcements":"/announcements",label:t("navAnnouncements"),Icon:FileText},{section:"documents",href:base?base+"/documents":"/documents",label:t("navDocuments"),Icon:FileText},...(manager?[{section:"people",href:base?base+"/people":"/people",label:t("people"),Icon:Users}]:[])];
 const home={href:"/",label:t("overview"),Icon:LayoutDashboard,active:path==="/"};
 const propertyLink={href:"/properties",label:t("properties"),Icon:Building2,active:path==="/properties"};
 const shortcuts=[home,propertyLink,...items.filter(item=>item.section==="units"||item.section==="people").map(item=>({...item,active:section===item.section}))];
 const searchable=[home,propertyLink,...items.map(item=>({...item,active:section===item.section}))];
 useEffect(()=>{function shortcut(event:KeyboardEvent){if((event.metaKey||event.ctrlKey)&&event.key.toLowerCase()==="k"){event.preventDefault();setSearchOpen(value=>!value);}}window.addEventListener("keydown",shortcut);return()=>window.removeEventListener("keydown",shortcut);},[]);
 const mainItems=items.filter(item=>["overview","buildings","units","people"].includes(item.section));
 const operationItems=items.filter(item=>!["overview","buildings","units","people"].includes(item.section));
 const planned=[{label:t("shellSharedAreas"),Icon:Plus},{label:t("shellParking"),Icon:Car},{label:t("shellSuppliers"),Icon:BriefcaseBusiness}];
 function links(values:typeof items){return values.map(item=><Link key={item.section} href={item.href} aria-label={item.label} title={item.label} className={section===item.section?"active":""} aria-current={section===item.section?"page":undefined} onNavigate={()=>setMenuOpen(false)}><item.Icon size={18} aria-hidden="true"/><span>{item.label}</span></Link>);}
 const navigation=<>
  <Link className="brand" href="/" onNavigate={()=>setMenuOpen(false)} aria-label={t("buildingflowOverview")}><BuildingFlowMark size={44} variant="light"/><span>Building<span className="brand-flow">Flow</span></span></Link>
  <div className="sidebar-scroll">
   <span className="label">{t("shellMainMenu")}</span>
   <nav aria-label={t("mainNavigation")}>{[home,propertyLink].map(item=><Link key={item.href} href={item.href} aria-label={item.label} title={item.label} className={item.active?"active":""} aria-current={item.active?"page":undefined} onNavigate={()=>setMenuOpen(false)}><item.Icon size={18} aria-hidden="true"/><span>{item.label}</span></Link>)}{links(mainItems)}{planned.map(item=><button key={item.label} className="sidebar-planned" disabled title={t("shellPlanned")}><item.Icon size={18} aria-hidden="true"/><span>{item.label}</span></button>)}</nav>
   <span className="label property-menu-title" title={scope.property?.name}>{t("shellOperations")}</span>
   <nav aria-label={scope.propertyId?t("propertyMenu"):t("managementModules")}>{links(operationItems)}<button className="sidebar-planned" disabled title={t("shellPlanned")}><Wrench size={18}/><span>{t("shellRequests")}</span></button></nav>
   {manager&&<><span className="label">{t("shellAdministration")}</span><nav aria-label={t("shellAdministration")}>{[{label:t("shellDefinitions"),Icon:SlidersHorizontal},{label:t("shellUsers"),Icon:Users},{label:t("shellSettings"),Icon:Settings}].map(item=><button key={item.label} className="sidebar-planned" disabled title={t("shellPlanned")}><item.Icon size={18}/><span>{item.label}</span>{item.Icon===SlidersHorizontal&&<ChevronDown size={14}/>}</button>)}</nav></>}
  </div>
  <div className="sidebar-footer"><strong>BuildingFlow</strong><span>{t("shellTagline")}</span><small>v1.0.0</small></div>
 </>;
 return <div className={`shell${collapsed?" shell-collapsed":""}`}>
  <a className="workspace-skip" href="#workspace-content">{t("skipToContent")}</a>
  <aside className="workspace-sidebar">{navigation}</aside>
  <main className="workspace-main">
   <header className="workspace-header"><div className="workspace-heading"><Button variant="ghost" size="icon" className="workspace-menu-button" aria-label={t("openNavigation")} aria-expanded={menuOpen||!collapsed} onClick={()=>{if(window.matchMedia("(min-width:1024px)").matches)setCollapsed(value=>!value);else setMenuOpen(true);}}><Menu aria-hidden="true"/></Button><button className="workspace-search" onClick={()=>setSearchOpen(true)}><Search size={18} aria-hidden="true"/><span>{t("shellSearch")}</span><kbd>⌘ K</kbd></button><span className="sr-only">{title}</span></div><SessionControls/></header>
   <div id="workspace-content" className="workspace-content" ref={content} role="region" aria-label={t("pageContent")} tabIndex={0}>{children}</div>
   <footer className="workspace-footer"><nav className="workspace-bottom-nav" aria-label={t("bottomNavigation")}>{shortcuts.map(item=><Link key={item.href} href={item.href} aria-label={item.label} title={item.label} className={item.active?"active":""} aria-current={item.active?"page":undefined}><item.Icon size={18} aria-hidden="true"/><span>{item.label}</span></Link>)}</nav><span className="workspace-footer-note">BuildingFlow</span></footer>
  </main>
  <Dialog open={searchOpen} onOpenChange={setSearchOpen}><DialogContent><DialogHeader><DialogTitle>{t("shellSearchTitle")}</DialogTitle><DialogDescription>{t("shellSearchDescription")}</DialogDescription></DialogHeader><input className="shell-search-input" aria-label={t("shellSearchTitle")} value={query} onChange={event=>setQuery(event.target.value)}/><nav className="shell-search-results">{searchable.filter(item=>item.label.toLocaleLowerCase().includes(query.toLocaleLowerCase())).map(item=><Link key={item.href} href={item.href} onNavigate={()=>{setSearchOpen(false);setQuery("");}}><item.Icon size={18}/>{item.label}</Link>)}{!searchable.some(item=>item.label.toLocaleLowerCase().includes(query.toLocaleLowerCase()))&&<p>{t("shellSearchEmpty")}</p>}</nav></DialogContent></Dialog>
  <Dialog open={menuOpen} onOpenChange={setMenuOpen}><DialogContent placement="right" className="workspace-mobile-menu left-0 right-auto flex max-w-xs flex-col gap-0 p-4 sm:max-w-xs data-[state=closed]:slide-out-to-left data-[state=open]:slide-in-from-left"><DialogHeader className="sr-only"><DialogTitle>{t("mainNavigation")}</DialogTitle><DialogDescription>{t("managementModules")}</DialogDescription></DialogHeader>{navigation}</DialogContent></Dialog>
 </div>;
}
