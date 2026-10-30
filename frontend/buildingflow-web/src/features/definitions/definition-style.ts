// Tanımlar ikonları: her liste değeri modülünün mevcut *-style.ts eşlemesinden gelir; eşlemesi olmayan sabit listeler burada.
import {ArrowDown,ArrowUp,Banknote,Building,Building2,BriefcaseBusiness,CalendarCheck2,CalendarDays,CalendarRange,Car,CircleX,CreditCard,DoorOpen,Factory,FileText,Hotel,House,Landmark,Layers,LayoutGrid,MessageSquareText,Minus,Repeat,Shapes,ShieldCheck,Store,Wallet,Warehouse,Wrench,type LucideIcon} from "lucide-react";
import type {Tone} from "../requests/category-style.ts";
import {categoryStyle as requestStyle} from "../requests/category-style.ts";
import {categoryStyle as expenseStyle} from "../expenses/category-style.ts";
import {categoryStyle as chargeStyle,kindStyle as chargeKindStyle} from "../dues/charge-kind-style.ts";
import {categoryStyle as announcementStyle} from "../communication/category-style.ts";
import {categoryStyle as surveyStyle} from "../surveys/category-style.ts";
import {typeStyle as maintenanceStyle} from "../maintenance/type-style.ts";
import {typeStyle as commonAreaStyle} from "../common-areas/type-style.ts";
import {spaceStyle,vehicleTypeIcon} from "../parking/parking-style.ts";
import {kindStyle as supplierKindStyle} from "../suppliers/supplier-style.ts";
import type {CatalogKey,DefinitionGroup,DefinitionKey,DefinitionLevel} from "./model.ts";

type Style={icon:LucideIcon;tone:Tone};
// Finans ve tedarikçi eşlemeleri WorkspacePresentation tonlarını kullanır; aynı görsel tona çevrilir.
const fromWorkspace={success:"green",warning:"amber",info:"blue",error:"red",violet:"violet",teal:"teal",neutral:"neutral"} as const satisfies Record<string,Tone>;
const workspace=<K extends string>(map:Record<K,{icon:LucideIcon;tone:keyof typeof fromWorkspace}>)=>Object.fromEntries(Object.entries<{icon:LucideIcon;tone:keyof typeof fromWorkspace}>(map).map(([k,v])=>[k,{icon:v.icon,tone:fromWorkspace[v.tone]}])) as Record<K,Style>;
const iconOnly=<K extends string>(map:Record<K,LucideIcon>,tone:Tone)=>Object.fromEntries(Object.entries<LucideIcon>(map).map(([k,icon])=>[k,{icon,tone}])) as Record<K,Style>;
const survey=Object.fromEntries(Object.entries(surveyStyle).map(([k,v])=>[k,{icon:v.icon,tone:"violet" as Tone}])) as Record<keyof typeof surveyStyle,Style>;

export const valueStyle:Record<DefinitionKey,Record<string,Style>>={
 PROPERTY_TYPE:{APARTMENT:{icon:Building,tone:"blue"},SITE:{icon:Building2,tone:"teal"},RESIDENCE:{icon:Hotel,tone:"violet"},MIXED:{icon:Store,tone:"amber"}},
 COMMON_AREA_TYPE:commonAreaStyle,
 UNIT_TYPE:{APARTMENT:{icon:House,tone:"blue"},OFFICE:{icon:BriefcaseBusiness,tone:"violet"},SHOP:{icon:Store,tone:"amber"},VILLA:{icon:House,tone:"green"},WAREHOUSE:{icon:Warehouse,tone:"neutral"},WORKSHOP:{icon:Factory,tone:"red"}},
 CHARGE_KIND:workspace(chargeKindStyle),
 CHARGE_CATEGORY:workspace(chargeStyle),
 EXPENSE_CATEGORY:expenseStyle,
 PAYMENT_METHOD:{CASH:{icon:Banknote,tone:"green"},BANK_TRANSFER:{icon:Landmark,tone:"blue"},ONLINE_CARD:{icon:CreditCard,tone:"violet"}},
 REQUEST_CATEGORY:requestStyle,
 REQUEST_PRIORITY:{LOW:{icon:ArrowDown,tone:"green"},NORMAL:{icon:Minus,tone:"blue"},HIGH:{icon:ArrowUp,tone:"red"}},
 MAINTENANCE_TYPE:maintenanceStyle,
 MAINTENANCE_FREQUENCY:{WEEKLY:{icon:CalendarDays,tone:"blue"},MONTHLY:{icon:CalendarDays,tone:"teal"},QUARTERLY:{icon:CalendarRange,tone:"violet"},SEMIANNUAL:{icon:CalendarRange,tone:"amber"},YEARLY:{icon:Repeat,tone:"green"}},
 VEHICLE_TYPE:iconOnly(vehicleTypeIcon,"blue"),
 PARKING_SPACE_TYPE:spaceStyle,
 SUPPLIER_KIND:workspace(supplierKindStyle),
 ANNOUNCEMENT_CATEGORY:announcementStyle,
 SURVEY_CATEGORY:survey,
};
export const fallbackStyle:Style={icon:Shapes,tone:"neutral"};
export const listIcon:Record<DefinitionKey,LucideIcon>={PROPERTY_TYPE:Building2,COMMON_AREA_TYPE:Shapes,UNIT_TYPE:DoorOpen,CHARGE_KIND:Wallet,CHARGE_CATEGORY:Wallet,EXPENSE_CATEGORY:Wallet,PAYMENT_METHOD:CreditCard,REQUEST_CATEGORY:Wrench,REQUEST_PRIORITY:ArrowUp,MAINTENANCE_TYPE:CalendarCheck2,MAINTENANCE_FREQUENCY:Repeat,VEHICLE_TYPE:Car,PARKING_SPACE_TYPE:Car,SUPPLIER_KIND:BriefcaseBusiness,ANNOUNCEMENT_CATEGORY:MessageSquareText,SURVEY_CATEGORY:MessageSquareText};
export const groupStyle:Record<DefinitionGroup,Style>={PROPERTY:{icon:Building2,tone:"blue"},UNIT:{icon:DoorOpen,tone:"teal"},FINANCE:{icon:Wallet,tone:"green"},REQUEST:{icon:Wrench,tone:"red"},MAINTENANCE:{icon:CalendarCheck2,tone:"amber"},PARKING:{icon:Car,tone:"blue"},SUPPLIER:{icon:BriefcaseBusiness,tone:"violet"},COMMUNICATION:{icon:MessageSquareText,tone:"teal"},DOCUMENT:{icon:FileText,tone:"neutral"}};
export const levelStyle:Record<DefinitionLevel,Style>={FIXED:{icon:ShieldCheck,tone:"neutral"},SYSTEM:{icon:Layers,tone:"blue"},PROPERTY:{icon:Building2,tone:"green"}};
export const catalogIcon:Record<CatalogKey,LucideIcon>={SUPPLIER_CATEGORIES:BriefcaseBusiness,DUES_CANCELLATION_REASONS:CircleX,CONSENT_TYPES:ShieldCheck,SMS_TEMPLATES:MessageSquareText,PAYMENT_PROVIDERS:CreditCard};
export const allIcon=LayoutGrid;
