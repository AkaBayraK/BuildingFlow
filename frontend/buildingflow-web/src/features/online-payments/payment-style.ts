// One icon and tone per online payment tab/state (AGENTS.md icon rule); the label always carries the meaning too.
import {AlertTriangle,ArrowLeftRight,Ban,BadgeCheck,BookOpenText,CircleCheck,CircleDashed,CircleX,Clock,CreditCard,FileWarning,Hourglass,Landmark,ListChecks,LockKeyhole,
 ReceiptText,Reply,RotateCcw,ShieldAlert,Undo2,WalletCards,type LucideIcon} from "lucide-react";
import type {Tone} from "@/components/management/WorkspacePresentation";
import type {BankAccountPurpose,BankAccountStatus,IntentStatus,IssueStatus,IssueType,LedgerType,RefundStatus} from "./model";
export type PaymentsTab="manual"|"online"|"ledger"|"reconciliation"|"settings";
export const tabIcon:Record<PaymentsTab,LucideIcon>={manual:WalletCards,online:CreditCard,ledger:BookOpenText,reconciliation:ListChecks,settings:Landmark};
export const intentStyle:Record<IntentStatus,{icon:LucideIcon;tone:Tone}>={PENDING:{icon:Hourglass,tone:"info"},SUCCEEDED:{icon:CircleCheck,tone:"success"},REVIEW:{icon:ShieldAlert,tone:"warning"},
 FAILED:{icon:CircleX,tone:"error"},EXPIRED:{icon:Clock,tone:"neutral"},CANCELLED:{icon:Ban,tone:"neutral"},REFUNDED:{icon:Undo2,tone:"violet"},CHARGED_BACK:{icon:RotateCcw,tone:"error"}};
export const bankStatusStyle:Record<BankAccountStatus,{icon:LucideIcon;tone:Tone}>={PENDING_APPROVAL:{icon:Hourglass,tone:"warning"},ACTIVE:{icon:BadgeCheck,tone:"success"},REJECTED:{icon:CircleX,tone:"error"},CLOSED:{icon:LockKeyhole,tone:"neutral"}};
export const purposeStyle:Record<BankAccountPurpose,{icon:LucideIcon;tone:Tone}>={ALL:{icon:Landmark,tone:"info"},DUES:{icon:ReceiptText,tone:"teal"},EXTRA:{icon:WalletCards,tone:"violet"}};
export const refundStyle:Record<RefundStatus,{icon:LucideIcon;tone:Tone}>={REQUESTED:{icon:Reply,tone:"warning"},REJECTED:{icon:Ban,tone:"neutral"},SUCCEEDED:{icon:Undo2,tone:"success"},FAILED:{icon:CircleX,tone:"error"}};
export const ledgerStyle:Record<LedgerType,{icon:LucideIcon;tone:Tone}>={COLLECTION:{icon:CircleCheck,tone:"success"},COLLECTION_REVERSAL:{icon:ArrowLeftRight,tone:"neutral"},UNALLOCATED_RECEIPT:{icon:CircleDashed,tone:"warning"},
 ONLINE_REFUND:{icon:Undo2,tone:"violet"},CHARGEBACK:{icon:RotateCcw,tone:"error"}};
export const issueStyle:Record<IssueType,{icon:LucideIcon;tone:Tone}>={DEBT_CHANGED:{icon:FileWarning,tone:"warning"},CHARGEBACK_UNREVERSED:{icon:RotateCcw,tone:"error"},PROVIDER_MISMATCH:{icon:AlertTriangle,tone:"error"},REFUND_UNCONFIRMED:{icon:Reply,tone:"warning"}};
export const issueStatusStyle:Record<IssueStatus,{icon:LucideIcon;tone:Tone}>={OPEN:{icon:AlertTriangle,tone:"warning"},RESOLVED:{icon:CircleCheck,tone:"success"}};
export const allIcon=ListChecks;
