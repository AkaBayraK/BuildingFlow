import {Receipt,BadgeCheck,Banknote,Scale,Layers,Zap,Users,Shield,Umbrella,Landmark,Repeat,CalendarDays,CalendarRange,CalendarCheck,CalendarX,AlarmClock,ArrowRight,ArrowLeft,Pin,TriangleAlert,SkipForward,Ban,Sparkles,Search,Siren,CalendarSync,ClipboardCheck,Play,Camera,Timer,ShieldCheck,Coins,ClipboardList,Building2,Wrench,Info,Paperclip,FileText,CalendarClock,XCircle,History,UserRound,FilePenLine,CheckCircle2,PauseCircle,ShieldBan,type LucideIcon} from "lucide-react";
import type {SupplierKind,SupplierStatus} from "./model";
import type {Tone} from "@/components/management/WorkspacePresentation";
export const kindStyle:Record<SupplierKind,{icon:LucideIcon;tone:Tone}>={COMPANY:{icon:Building2,tone:"info"},INDIVIDUAL:{icon:UserRound,tone:"violet"}};
export const statusStyle:Record<SupplierStatus,{icon:LucideIcon;tone:Tone}>={DRAFT:{icon:FilePenLine,tone:"neutral"},ACTIVE:{icon:CheckCircle2,tone:"success"},INACTIVE:{icon:PauseCircle,tone:"warning"},BLOCKED:{icon:ShieldBan,tone:"error"}};

export const relationStyle={area:{icon:Building2},category:{icon:Wrench}} as const;

export const supplierTabs={about:{icon:Info},services:{icon:Wrench},documents:{icon:Paperclip},contracts:{icon:FileText},quotes:{icon:ClipboardList},workOrders:{icon:ClipboardCheck},maintenance:{icon:CalendarSync},invoices:{icon:Receipt}} as const;
export const contractStyle={DRAFT:{icon:FilePenLine,tone:"neutral"},ACTIVE:{icon:CheckCircle2,tone:"success"},UPCOMING:{icon:CalendarClock,tone:"info"},EXPIRED:{icon:History,tone:"warning"},CANCELLED:{icon:XCircle,tone:"error"}} as const;

export const renewalStyle={icon:CalendarClock} as const;
export const contractActionStyle={CREATE:{icon:FilePenLine},EDIT:{icon:FilePenLine},ACTIVATE:{icon:CheckCircle2},CANCEL:{icon:XCircle}} as const;

export const quoteStyle={OPEN:{icon:FilePenLine,tone:"info"},AWARDED:{icon:CheckCircle2,tone:"success"},CANCELLED:{icon:XCircle,tone:"error"},AWAITING:{icon:CalendarClock,tone:"neutral"},PENDING:{icon:FilePenLine,tone:"info"},APPROVED:{icon:CheckCircle2,tone:"success"},REJECTED:{icon:XCircle,tone:"error"},NOT_SELECTED:{icon:PauseCircle,tone:"neutral"}} as const;
export const currencyStyle={TRY:{icon:Coins},USD:{icon:Coins},EUR:{icon:Coins}} as const;
export const inquiryActionStyle={CREATE:{icon:FilePenLine},QUOTE:{icon:FileText},APPROVE:{icon:CheckCircle2},REJECT:{icon:XCircle},CANCEL:{icon:XCircle},CONVERT:{icon:ClipboardList}} as const;

export const workOrderStyle={DRAFT:{icon:FilePenLine,tone:"neutral"},ASSIGNED:{icon:ClipboardList,tone:"info"},ACCEPTED:{icon:CheckCircle2,tone:"info"},IN_PROGRESS:{icon:Play,tone:"warning"},COMPLETED:{icon:CheckCircle2,tone:"success"},VERIFIED:{icon:ShieldCheck,tone:"success"},CLOSED:{icon:ClipboardCheck,tone:"success"},REJECTED:{icon:ShieldBan,tone:"error"},CANCELLED:{icon:XCircle,tone:"error"}} as const;
export const workOrderSourceStyle={QUOTE:{icon:FileText},REQUEST:{icon:Wrench},MAINTENANCE:{icon:CalendarClock}} as const;
export const workOrderActionStyle={...workOrderStyle,CREATE:{icon:FilePenLine},EDIT:{icon:FilePenLine},PHOTO:{icon:Camera}} as const;
export const workOrderSlaStyle={icon:Timer} as const;

export const maintenanceFrequencyStyle={WEEKLY:{icon:Repeat},MONTHLY:{icon:CalendarDays},QUARTERLY:{icon:CalendarRange},SEMIANNUAL:{icon:CalendarRange},YEARLY:{icon:CalendarCheck}} as const;
export const maintenanceRuleStyle={NEXT_WORKING_DAY:{icon:ArrowRight},PREVIOUS_WORKING_DAY:{icon:ArrowLeft},KEEP:{icon:Pin}} as const;
export const maintenanceTypeStyle={MAINTENANCE:{icon:Wrench},CLEANING:{icon:Sparkles},INSPECTION:{icon:Search},DRILL:{icon:Siren},OTHER:{icon:Info}} as const;
export const scheduleStyle={ACTIVE:{icon:CalendarSync,tone:"success"},ENDED:{icon:Ban,tone:"neutral"}} as const;
export const occurrenceStyle={PLANNED:{icon:CalendarDays,tone:"info"},COMPLETED:{icon:CalendarCheck,tone:"success"},SKIPPED:{icon:SkipForward,tone:"neutral"},OVERDUE:{icon:TriangleAlert,tone:"warning"},SHIFTED:{icon:ArrowRight,tone:"info"},CONFLICT:{icon:CalendarX,tone:"error"}} as const;
export const maintenanceActionStyle={CREATE:{icon:FilePenLine},END:{icon:Ban},COMPLETE:{icon:CheckCircle2},SKIP:{icon:SkipForward}} as const;
export const maintenanceReminderStyle={icon:AlarmClock} as const;

export const invoiceStyle={ALL:{icon:Layers,tone:"neutral"},RECORDED:{icon:FilePenLine,tone:"info"},APPROVED:{icon:BadgeCheck,tone:"success"},PAID:{icon:Banknote,tone:"success"},RECONCILED:{icon:Scale,tone:"success"},CANCELLED:{icon:XCircle,tone:"error"}} as const;
export const invoiceActionStyle={CREATE:{icon:FilePenLine},APPROVE:{icon:BadgeCheck},PAY:{icon:Banknote},RECONCILE:{icon:Scale},CANCEL:{icon:XCircle}} as const;
export const invoiceCategoryStyle={MAINTENANCE:{icon:Wrench},CLEANING:{icon:Sparkles},STAFF:{icon:Users},ENERGY:{icon:Zap},SECURITY:{icon:Shield},INSURANCE:{icon:Umbrella},ADMINISTRATION:{icon:Landmark},OTHER:{icon:Info}} as const;
export const invoiceOverdueStyle={icon:TriangleAlert} as const;
