"use client";
import Image from "next/image";
import Link from "next/link";
import {usePathname} from "next/navigation";
import {useTranslations} from "next-intl";
import {Building2,LayoutDashboard,DoorOpen,Users,FileText,ReceiptText,WalletCards,ChartNoAxesCombined} from "lucide-react";
import {SessionControls,useIdentity} from "@/auth/AuthGate";
import {PropertyScopeProvider,usePropertyScope} from "@/features/navigation/PropertyScope";
import {managementSection} from "@/features/navigation/routes";
export function WorkspaceShell({children}:{children:React.ReactNode}){
 return <PropertyScopeProvider><Shell>{children}</Shell></PropertyScopeProvider>;
}
function Shell({children}:{children:React.ReactNode}){
 const t=useTranslations("ui");const path=usePathname();const scope=usePropertyScope();const identity=useIdentity();const properties=path.startsWith("/properties");
 const finance=["SUPER_ADMIN","SITE_ADMIN","MANAGER","ACCOUNTANT"].some(role=>identity.hasRealmRole(role));
 const manager=["SUPER_ADMIN","SITE_ADMIN","MANAGER"].some(role=>identity.hasRealmRole(role));
 const section=scope.propertyId?managementSection(path,scope.propertyId):path==="/units"?"units":path==="/people"?"people":path==="/dues"?"dues":path==="/payments"?"payments":path==="/expenses"?"expenses":path==="/announcements"?"announcements":path==="/documents"?"documents":path==="/meters"?"meters":path==="/reports"?"reports":null;
 const title=section==="meters"?t("navMeters"):section==="reports"?t("navReports"):section==="announcements"?t("navAnnouncements"):section==="documents"?t("navDocuments"):section==="expenses"?t("navExpenses"):section==="payments"?t("navPayments"):section==="dues"?t("navDues"):section==="units"?t("navUnits"):section==="buildings"?t("navBuildings"):section==="people"?t("people"):scope.propertyId?t("navOverview"):properties?t("properties"):t("overview");
 const base=scope.propertyId?`/properties/${scope.propertyId}`:null;
 const items=[...(base?[{section:"overview",href:base,label:t("navOverview"),Icon:FileText}]:[]),...(base && scope.property && scope.property.type!=="APARTMENT"?[{section:"buildings",href:base+"/buildings",label:t("navBuildings"),Icon:Building2}]:[]),{section:"units",href:base?base+"/units":"/units",label:scope.property?.type==="APARTMENT"?t("apartments"):t("navUnits"),Icon:DoorOpen},...(finance?[{section:"meters",href:base?base+"/meters":"/meters",label:t("navMeters"),Icon:ChartNoAxesCombined},{section:"reports",href:base?base+"/reports":"/reports",label:t("navReports"),Icon:ChartNoAxesCombined},{section:"dues",href:base?base+"/dues":"/dues",label:t("navDues"),Icon:ReceiptText},{section:"payments",href:base?base+"/payments":"/payments",label:t("navPayments"),Icon:WalletCards},{section:"expenses",href:base?base+"/expenses":"/expenses",label:t("navExpenses"),Icon:ChartNoAxesCombined}]:[]),{section:"announcements",href:base?base+"/announcements":"/announcements",label:t("navAnnouncements"),Icon:FileText},{section:"documents",href:base?base+"/documents":"/documents",label:t("navDocuments"),Icon:FileText},...(manager?[{section:"people",href:base?base+"/people":"/people",label:t("people"),Icon:Users}]:[])];
 return <div className="shell"><aside><Link className="brand" href="/" aria-label={t("buildingflowOverview")}><Image src="/buildingflow-logo.png" alt="BuildingFlow" width={174} height={174} priority/></Link><span className="label">{t("managementAREA")}</span><nav aria-label={t("mainNavigation")}><Link className={path==="/"?"active":""} aria-current={path==="/"?"page":undefined} href="/"><LayoutDashboard size={18}/><span>{t("overview")}</span></Link><Link className={path==="/properties"?"active":""} aria-current={path==="/properties"?"page":undefined} href="/properties"><Building2 size={18}/><span>{t("properties")}</span></Link></nav>
 <span className="label property-menu-title" title={scope.property?.name}>{scope.property?.name ?? t("managementModules")}</span><nav aria-label={scope.propertyId?t("propertyMenu"):t("managementModules")}>{items.map(item=><Link key={item.section} href={item.href} className={section===item.section?"active":""} aria-current={section===item.section?"page":undefined}><item.Icon size={18}/><span>{item.label}</span></Link>)}</nav>
 {!scope.propertyId && <p className="sidebar-note">{t("selectPropertyForMenus")}</p>}
 <p className="sidebar-note">{t("yourCommunities")}<br/><span>{t("betterOrganizedTogether")}</span></p><div className="sidebar-footer">BuildingFlow <span>{t("phase11Version")}</span></div></aside><main><header><span className="workspace-breadcrumb">{t("workspaceBreadcrumb")}<strong>{title}</strong></span><SessionControls/></header>{children}<footer>{t("buildingflowPropertiesAndUnits")} <span>{t("betterOrganizedTogether")}</span></footer></main></div>;
}
