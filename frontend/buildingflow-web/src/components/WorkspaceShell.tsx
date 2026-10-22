"use client";
import Link from "next/link";
import {DashboardAreaProvider,DashboardAreaPicker} from "@/features/dashboard/DashboardArea";
import {Tooltip} from "radix-ui";
import {sidebarRows} from "@/features/navigation/sidebar-layout";
import {SidebarHint} from "@/features/navigation/SidebarHint";
import {readSidebarPreference,writeSidebarPreference} from "@/features/navigation/sidebar-preference";
import {BuildingFlowMark} from "@/components/BuildingFlowMark";
import {useEffect,useRef,useState} from "react";
import {usePathname} from "next/navigation";
import {useLocale,useTranslations} from "next-intl";
import {searchNavigation} from "@/features/navigation/search";
import {Building2,CalendarCheck2,LayoutDashboard,DoorOpen,Users,FileText,ReceiptText,WalletCards,ChartNoAxesCombined,Menu,Search,ChevronDown,Plus,Car,BriefcaseBusiness,Wrench,Settings,SlidersHorizontal,MessageSquareText,UserCog,ShieldCheck,UserRound,ListChecks,ClipboardList} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Dialog,DialogContent,DialogHeader,DialogTitle,DialogDescription} from "@/components/ui/dialog";
import {SessionControls,SessionProfileMenu,SessionProfileProvider,useIdentity} from "@/auth/AuthGate";
import {PropertyScopeProvider,usePropertyScope} from "@/features/navigation/PropertyScope";
import {createNavigation,navigationRoles,type NavigationItem} from "@/features/navigation/navigation";
const navigationIcons={Building2,CalendarCheck2,LayoutDashboard,DoorOpen,Users,FileText,ReceiptText,WalletCards,ChartNoAxesCombined,Plus,Car,BriefcaseBusiness,Wrench,Settings,SlidersHorizontal,MessageSquareText,UserCog,ShieldCheck,UserRound,ListChecks,ClipboardList};
import {pickSlogan} from "@/features/dashboard/slogans";
import {StatusFooter} from "@/features/system-status/StatusFooter";
export function WorkspaceShell(props:{children:React.ReactNode;centralPeople?:boolean;sessionContexts?:boolean}){return <SessionProfileProvider><DashboardAreaProvider><WorkspaceContent {...props}/></DashboardAreaProvider></SessionProfileProvider>;}
function WorkspaceContent({children,centralPeople=false,sessionContexts=false}:{children:React.ReactNode;centralPeople?:boolean;sessionContexts?:boolean}){
 const path=usePathname();
 if(sessionContexts&&(path==="/"||path.startsWith("/workspaces")))return <ContextShell>{children}</ContextShell>;
 return <PropertyScopeProvider><Shell centralPeople={centralPeople} sessionContexts={sessionContexts}>{children}</Shell></PropertyScopeProvider>;
}
function Shell({children,centralPeople,sessionContexts}:{children:React.ReactNode;centralPeople:boolean;sessionContexts:boolean}){
 const menuButton=useRef<HTMLButtonElement>(null);const mobilePanel=useRef<HTMLDivElement>(null);const returnToContent=useRef(false);
 const [desktop,setDesktop]=useState(false);const [openGroup,setOpenGroup]=useState<string|null>(null);const [menuOpen,setMenuOpen]=useState(false);const [collapsed,setCollapsed]=useState(false);const [searchOpen,setSearchOpen]=useState(false);const [query,setQuery]=useState("");const content=useRef<HTMLDivElement>(null);const [slogan,setSlogan]=useState(0);
 const t=useTranslations("ui"),w=useTranslations("sessionWorkspaces"),locale=useLocale();const path=usePathname();const scope=usePropertyScope();const identity=useIdentity();
 useEffect(()=>{content.current?.scrollTo({top:0,left:0});returnToContent.current=true;setMenuOpen(false);},[path]);
 // İŞ20: one slogan per page load, chosen after hydration so server and client markup match.
 useEffect(()=>{setSlogan(pickSlogan(Math.random()));},[]);
 useEffect(()=>{const media=window.matchMedia("(min-width:1024px)");const sync=()=>{setDesktop(media.matches);if(media.matches){returnToContent.current=true;setMenuOpen(false);}};sync();media.addEventListener("change",sync);try{setCollapsed(readSidebarPreference(window.localStorage));}catch{}return()=>media.removeEventListener("change",sync);},[]);
 function closeForNavigation(){returnToContent.current=true;setMenuOpen(false);}
 function openMenuSearch(){returnToContent.current=false;setMenuOpen(false);setSearchOpen(true);}
 function toggleCollapsed(){const next=!collapsed;setCollapsed(next);try{writeSidebarPreference(window.localStorage,next);}catch{}}
 useEffect(()=>{if(!searchOpen)setQuery("");},[searchOpen]);
 const model=createNavigation({roles:navigationRoles.filter(role=>identity.hasRealmRole(role)),centralPeople,sessionContexts,propertyId:scope.propertyId,property:scope.property},path);
 const label=(value:NavigationItem["label"])=>value.namespace==="ui"?t(value.key):w(value.key);
 function present<T extends NavigationItem>(item:T){return {...item,label:label(item.label),Icon:navigationIcons[item.icon]};}
 const home=present(model.home);
 const sidebar=sidebarRows(model);
 const activeGroup=sidebar.find(row=>row.kind==="group"&&row.items.some(item=>item.active))?.id??null;
 useEffect(()=>{setOpenGroup(activeGroup);},[path,scope.propertyId,activeGroup]);
 const results=searchNavigation(model.searchable,query,item=>label(item.label),locale).map(present);
 function changeSearchOpen(open:boolean){setSearchOpen(open);if(!open)setQuery("");}
 const activeItem=model.groups.flatMap(group=>group.items).find(item=>item.active)??[model.account,model.home].find(item=>item.active);
 const title=activeItem?label(activeItem.label):t("overview");
 useEffect(()=>{function shortcut(event:KeyboardEvent){if((event.metaKey||event.ctrlKey)&&event.key.toLowerCase()==="k"){event.preventDefault();returnToContent.current=false;setMenuOpen(false);setSearchOpen(value=>!value);}}window.addEventListener("keydown",shortcut);return()=>window.removeEventListener("keydown",shortcut);},[]);
 function links(values:readonly NavigationItem[],mobile:boolean){return values.map(value=>{const item=present(value);return item.disabled?<button key={item.id} className="sidebar-planned" disabled aria-label={`${item.label} — ${t("shellPlanned")}`} title={`${item.label} — ${t("shellPlanned")}`}><item.Icon size={18} aria-hidden="true"/><span>{item.label}</span></button>:<SidebarHint key={item.id} enabled={desktop&&collapsed&&!mobile} label={item.label}><Link href={item.href} aria-label={item.label} title={collapsed&&!mobile?undefined:item.label} className={item.active?"active":""} aria-current={item.active?"page":undefined} onNavigate={closeForNavigation}><item.Icon size={18} aria-hidden="true"/><span>{item.label}</span></Link></SidebarHint>;});}
 const navigation=(mobile:boolean)=><>
  <Link className="brand" href="/dashboard" onNavigate={closeForNavigation} aria-label={t("buildingflowOverview")}><BuildingFlowMark size={44} variant="light"/><span>Building<span className="brand-flow">Flow</span></span></Link>
  <button type="button" className="sidebar-search-trigger" aria-label={t("shellSearchTitle")} onClick={openMenuSearch}><Search size={18} aria-hidden="true"/><span>{t("sidebarSearchPlaceholder")}</span><kbd>⌘ K</kbd></button>
  <div className="sidebar-scroll">
   <nav className="sidebar-primary" aria-label={t("mainNavigation")}>{sidebar.map(row=>{
    if(row.kind==="link")return <div key={row.id} className={`sidebar-direct sidebar-direct-${row.id}`}>{links([row.item],mobile)}</div>;
    const open=collapsed&&!mobile||openGroup===row.id,id=`${mobile?"mobile":"desktop"}-navigation-${row.id}`,Icon=navigationIcons[row.icon];
    return <div key={row.id} className={`sidebar-group sidebar-tree-group${open?" is-open":""}`}><button type="button" className="sidebar-group-toggle sidebar-tree-toggle" aria-expanded={open} aria-controls={id} onClick={()=>setOpenGroup(value=>value===row.id?null:row.id)}><Icon size={20} aria-hidden="true"/><span>{label(row.label)}</span><ChevronDown size={14} aria-hidden="true"/></button><div className="sidebar-tree-items" id={id} hidden={!open}>{links(row.items,mobile)}</div></div>;
   })}</nav>
   <nav className="sidebar-end" aria-label={home.label}><SidebarHint enabled={desktop&&collapsed&&!mobile} label={home.label}><Link href={home.href} aria-label={home.label} title={collapsed&&!mobile?undefined:home.label} className={home.active?"active":""} aria-current={home.active?"page":undefined} onNavigate={closeForNavigation}><home.Icon size={18} aria-hidden="true"/><span>{home.label}</span></Link></SidebarHint></nav>
  </div>
  <div className="navigation-footer">
   <SessionProfileMenu sidebar compact={collapsed&&!mobile} onNavigate={closeForNavigation}/>
  </div>
 </>;
 return <Tooltip.Provider delayDuration={250}><div className={`shell${collapsed?" shell-collapsed":""}`}>
  <a className="workspace-skip" href="#workspace-content">{t("skipToContent")}</a>
  <aside id="workspace-navigation" className="workspace-sidebar">{navigation(false)}</aside>
  <main className="workspace-main">
   <header className="workspace-header"><div className="workspace-heading"><Button ref={menuButton} variant="ghost" size="icon" className="workspace-menu-button" aria-label={desktop?t(collapsed?"sidebarExpand":"sidebarCollapse"):t("openNavigation")} aria-expanded={desktop?!collapsed:menuOpen} aria-controls={desktop?"workspace-navigation":"mobile-workspace-navigation"} onClick={()=>{if(desktop)toggleCollapsed();else {returnToContent.current=false;setMenuOpen(true);}}}><Menu aria-hidden="true"/></Button><button className="workspace-search" aria-label={t("shellSearchTitle")} onClick={openMenuSearch}><Search size={18} aria-hidden="true"/><span>{t("shellSearch")}</span><kbd>⌘ K</kbd></button><p className="workspace-slogan" aria-live="off">{slogan?t(`shellSlogan${slogan}` as `shellSlogan${1|2|3|4|5|6|7|8}`):""}</p><span className="sr-only">{title}</span></div><SessionControls beforeLanguage={<DashboardAreaPicker/>}/></header>
   <div id="workspace-content" className="workspace-content" ref={content} role="region" aria-label={t("pageContent")} tabIndex={0}>{children}</div>
   <footer className="workspace-footer flex min-h-12! py-1.5!" aria-label={t("bottomNavigation")}><StatusFooter/></footer>
  </main>
  <Dialog open={searchOpen} onOpenChange={changeSearchOpen}><DialogContent onCloseAutoFocus={event=>{if(!desktop){event.preventDefault();(returnToContent.current?content.current:menuButton.current)?.focus();}}}><DialogHeader><DialogTitle>{t("shellSearchTitle")}</DialogTitle><DialogDescription>{t("shellSearchDescription")}</DialogDescription></DialogHeader><input className="shell-search-input" type="search" autoComplete="off" spellCheck={false} aria-label={t("shellSearchTitle")} aria-controls="navigation-search-results" value={query} onChange={event=>setQuery(event.target.value)}/><nav id="navigation-search-results" className="shell-search-results" aria-label={t("shellSearchTitle")}>{results.map(item=><Link key={item.id} href={item.href} onNavigate={()=>{returnToContent.current=true;changeSearchOpen(false);}}><item.Icon size={18}/>{item.label}</Link>)}{results.length===0&&<p role="status">{t("shellSearchEmpty")}</p>}</nav></DialogContent></Dialog>
  <Dialog open={menuOpen&&!desktop} onOpenChange={setMenuOpen}><DialogContent ref={mobilePanel} id="mobile-workspace-navigation" onOpenAutoFocus={event=>{const target=mobilePanel.current?.querySelector<HTMLElement>('a[aria-current="page"]')??mobilePanel.current?.querySelector<HTMLElement>('a[href]');if(target){event.preventDefault();target.focus();}}} onCloseAutoFocus={event=>{event.preventDefault();if(searchOpen)return;const target=returnToContent.current?content.current:menuButton.current;if(target?.isConnected)target.focus();}} placement="right" className="workspace-mobile-menu left-0 right-auto flex max-w-xs flex-col gap-0 p-4 sm:max-w-xs data-[state=closed]:slide-out-to-left data-[state=open]:slide-in-from-left"><DialogHeader className="sr-only"><DialogTitle>{t("mainNavigation")}</DialogTitle><DialogDescription>{t("managementModules")}</DialogDescription></DialogHeader>{navigation(true)}</DialogContent></Dialog>
 </div></Tooltip.Provider>;
}

function ContextShell({children}:{children:React.ReactNode}){const w=useTranslations("sessionWorkspaces"),ui=useTranslations("ui");return <div className="min-h-screen bg-background text-foreground"><header className="flex flex-wrap items-center justify-between gap-4 border-b bg-card px-5 py-4"><Link href="/workspaces" className="flex items-center gap-3 font-semibold"><BuildingFlowMark size={36}/><span>BuildingFlow</span></Link><SessionControls beforeLanguage={<DashboardAreaPicker/>}/></header><nav aria-label={w("navigation")} className="flex gap-4 border-b px-5 py-3"><Link className="min-h-11 content-center text-primary underline" href="/workspaces">{w("title")}</Link><Link className="min-h-11 content-center text-primary underline" href="/workspaces/portfolio">{w("portfolio")}</Link><Link className="min-h-11 content-center text-primary underline" href="/account">{ui("navAccount")}</Link></nav><main className="px-5">{children}</main></div>;}
