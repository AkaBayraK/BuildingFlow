import {managementSection, propertyIdFromPath, type ManagementSection} from "./routes.ts";

export const navigationRoles = ["SUPER_ADMIN", "SITE_ADMIN", "MANAGER", "ACCOUNTANT"] as const;
type Role = typeof navigationRoles[number];
export type NavigationContext = {
  roles: readonly string[];
  sessionContexts: boolean;
  centralPeople: boolean;
  propertyId: string | null;
  property: {id: string; type: "APARTMENT" | "SITE" | "RESIDENCE" | "MIXED"} | null;
};
export type NavigationIcon = "Building2" | "CalendarCheck2" | "LayoutDashboard" | "DoorOpen" | "Users" | "FileText" | "ReceiptText" | "WalletCards" | "ChartNoAxesCombined" | "Plus" | "Car" | "BriefcaseBusiness" | "Wrench" | "Settings" | "SlidersHorizontal" | "MessageSquareText" | "UserCog" | "ShieldCheck" | "UserRound" | "ListChecks" | "ClipboardList";
type LabelKey = "sidebarUnitsGroup" | "sidebarAdminGroup" | "navigationOverviewGroup" | "navigationPropertyGroup" | "navigationFinanceGroup" | "navigationOperationsGroup" | "navigationCommunicationGroup" | "navigationSystemGroup" | "navDashboard" | "properties" | "navOverview" | "navBuildings" | "apartments" | "navUnits" | "navMeters" | "navMaintenance" | "navReports" | "navDues" | "navPayments" | "navExpenses" | "navAnnouncements" | "navSurveys" | "navDocuments" | "navWebsite" | "shellRequests" | "people" | "navSms" | "navUsers" | "navConsents" | "navAccount" | "overview" | "shellSharedAreas" | "shellParking" | "shellSuppliers" | "shellDefinitions" | "shellUsers" | "shellSettings";
export type NavigationLabel = {namespace: "ui"; key: LabelKey} | {namespace: "sessionWorkspaces"; key: "title"};
type ItemBase = {
  id: string;
  label: NavigationLabel;
  icon: NavigationIcon;
  section?: ManagementSection;
  active: boolean;
  children?: readonly NavigationItem[];
};
export type NavigationItem = ItemBase & ({disabled: true; href?: never} | {disabled: false; href: string});
export type NavigationLink = Extract<NavigationItem, {disabled: false}>;
export type NavigationGroup = {id: "overview" | "property" | "finance" | "operations" | "communication" | "administration"; label: NavigationLabel; items: readonly NavigationItem[]};
export type NavigationModel = {groups: readonly NavigationGroup[]; dashboard: NavigationLink; propertyLink: NavigationLink; home: NavigationLink; account: NavigationLink; shortcuts: readonly NavigationLink[]; searchable: readonly NavigationLink[]};

// Visibility mirrors the existing shell. It is never a backend permission or proof of membership.
export function navigationPermissions(context: NavigationContext) {
  const has = (role: Role) => context.roles.includes(role);
  const platform = has("SUPER_ADMIN");
  const manager = platform || has("SITE_ADMIN") || has("MANAGER");
  return {manager, finance: manager || has("ACCOUNTANT"), users: platform || !context.sessionContexts && has("SITE_ADMIN"), administration: manager && (!context.sessionContexts || platform)};
}

export function isNavigationActive(item: NavigationItem, path: string): boolean {
  if (item.disabled) return false;
  if (item.children?.some(child => isNavigationActive(child, path))) return true;
  const propertyId = propertyIdFromPath(item.href);
  if (propertyId && item.section) {
    if (propertyIdFromPath(path)?.toLowerCase() !== propertyId.toLowerCase()) return false;
    const suffix = path.slice(`/properties/${propertyId}`.length);
    if (item.section === "overview") return suffix === "" || suffix === "/";
    if (!/^\/(buildings|units|people|dues|payments|expenses|announcements|surveys|documents|meters|maintenance|reports|sms|requests|website)(?:\/|$)/.test(suffix)) return false;
    return managementSection(path, propertyId) === item.section;
  }
  if (item.href === "/people" || item.href === "/consents" || item.href === "/workspaces") return path === item.href || path.startsWith(item.href + "/");
  return path === item.href;
}

export function navigationDuplicates(items: readonly NavigationItem[]): string[] {
  const ids = new Set<string>(), routes = new Set<string>(), duplicates: string[] = [];
  function visit(values: readonly NavigationItem[]) {
    for (const item of values) {
      if (ids.has(item.id)) duplicates.push(`id:${item.id}`);
      ids.add(item.id);
      if (!item.disabled) {
        const route = item.href === "/" ? "/" : item.href.replace(/\/+$/, "").toLowerCase();
        if (routes.has(route)) duplicates.push(`route:${route}`);
        routes.add(route);
      }
      if (item.children) visit(item.children);
    }
  }
  visit(items);
  return duplicates;
}

export function createNavigation(context: NavigationContext, path: string): NavigationModel {
  const permissions = navigationPermissions(context);
  // Only URL-derived UUIDs and a matching loaded property influence scoped destinations/type.
  const propertyId = context.propertyId && propertyIdFromPath(`/properties/${context.propertyId}`) === context.propertyId ? context.propertyId : null;
  const base = propertyId ? `/properties/${propertyId}` : null;
  const property = context.property?.id === propertyId ? context.property : null;
  function link(id: string, href: string, key: LabelKey, icon: NavigationIcon, section?: ManagementSection): NavigationLink {
    const item: NavigationLink = {id, href, label: {namespace: "ui", key}, icon, section, active: false, disabled: false};
    return {...item, active: isNavigationActive(item, path)};
  }
  const planned = (id: string, key: LabelKey, icon: NavigationIcon): NavigationItem => ({id, label: {namespace: "ui", key}, icon, disabled: true, active: false});
  const module = (id: ManagementSection, key: LabelKey, icon: NavigationIcon) => link(id, `${base ?? ""}/${id}`, key, icon, id);
  const dashboard = link("dashboard", "/dashboard", "navDashboard", "LayoutDashboard");
  const propertyLink = link("properties", "/properties", "properties", "Building2");
  const units = module("units", property?.type === "APARTMENT" ? "apartments" : "navUnits", "DoorOpen");
  const people = permissions.manager ? link("people", context.centralPeople ? "/people" : `${base ?? ""}/people`, "people", "Users", "people") : null;
  const main: NavigationItem[] = [dashboard, propertyLink, ...(base ? [link("overview", base, "navOverview", "FileText", "overview")] : []), ...(base && property && property.type !== "APARTMENT" ? [module("buildings", "navBuildings", "Building2")] : []), units, ...(people ? [people] : []), planned("shared-areas", "shellSharedAreas", "Plus"), planned("parking", "shellParking", "Car"), planned("suppliers", "shellSuppliers", "BriefcaseBusiness")];
  const operations: NavigationItem[] = [
    ...(permissions.finance ? [module("maintenance", "navMaintenance", "CalendarCheck2"), module("meters", "navMeters", "ChartNoAxesCombined"), module("reports", "navReports", "ChartNoAxesCombined"), module("dues", "navDues", "ReceiptText"), module("payments", "navPayments", "WalletCards"), module("expenses", "navExpenses", "ChartNoAxesCombined")] : []),
    module("announcements", "navAnnouncements", "FileText"), module("documents", "navDocuments", "FileText"),
    ...(permissions.manager ? [module("surveys", "navSurveys", "ClipboardList"), module("website", "navWebsite", "Building2"), module("requests", "shellRequests", "Wrench"), module("sms", "navSms", "MessageSquareText")] : []),
    ...(permissions.users ? [link("users", "/users", "navUsers", "UserCog")] : [])
  ];
  const administration = permissions.administration ? [link("consents", "/consents", "navConsents", "ShieldCheck"), planned("definitions", "shellDefinitions", "SlidersHorizontal"), link("settings", "/system-settings", "shellSettings", "Settings")] : [];
  const home: NavigationLink = context.sessionContexts ? {...link("home", "/workspaces", "overview", "ListChecks"), label: {namespace: "sessionWorkspaces", key: "title"}} : link("home", "/", "overview", "ListChecks");
  const account = link("account", "/account", "navAccount", "UserRound");
  const visibleItems = [...main, ...operations, ...administration];
  function group(id: NavigationGroup["id"], key: LabelKey, ids: readonly string[]): NavigationGroup {
    return {id, label: {namespace: "ui", key}, items: ids.flatMap(id => visibleItems.filter(item => item.id === id))};
  }
  const groups = [
    group("overview", "navigationOverviewGroup", ["dashboard"]),
    group("property", "navigationPropertyGroup", ["properties", "overview", "buildings", "units", "people", "shared-areas", "parking"]),
    group("finance", "navigationFinanceGroup", ["dues", "payments", "expenses", "reports"]),
    group("operations", "navigationOperationsGroup", ["requests", "maintenance", "meters", "suppliers"]),
    group("communication", "navigationCommunicationGroup", ["announcements", "surveys", "documents", "website", "sms"]),
    group("administration", "navigationSystemGroup", ["users", "consents", "definitions", "settings"])
  ].filter(group => group.items.length > 0);

  return {groups, dashboard, propertyLink, home, account, shortcuts: [dashboard, propertyLink, units, ...(people ? [people] : [])], searchable: [...groups.flatMap(group => group.items).filter(item => !item.disabled), account, home]};
}

// Reuse the existing module entry pages to choose another authorized property.
export function propertySelectionHref(path:string,propertyId:string|null):string{
 const modules:readonly ManagementSection[]=["units","people","dues","payments","expenses","announcements","surveys","documents","meters","maintenance","reports","sms","requests","website"];
 if(propertyId){
  if(propertyIdFromPath(path)!==propertyId)return "/properties";
  const section=managementSection(path,propertyId);
  return modules.includes(section)?`/${section}`:"/properties";
 }
 const section=modules.find(section=>path===`/${section}`||section==="people"&&path.startsWith("/people/"));
 return section?`/${section}`:"/properties";
}
