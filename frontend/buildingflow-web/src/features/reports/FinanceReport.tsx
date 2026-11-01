"use client";
import {useEffect,useState} from "react";
import {useLocale,useTranslations} from "next-intl";
import {ArrowUpRight,CalendarClock,CalendarDays,CalendarMinus,CalendarRange,CalendarSearch,FileDown,Printer,ReceiptText,Scale,Table2,WalletCards,type LucideIcon} from "lucide-react";
import {useAuthorizedApi,errorMessage} from "@/services/api";
import {businessToday,formatDecimalCurrency,type Locale} from "@/lib/format";
import {Button} from "@/components/ui/button";
import {Input} from "@/components/ui/input";
import {Label} from "@/components/ui/label";
import {FilterChips} from "@/components/management/FilterChips";
import {SummaryCard} from "@/components/management/WorkspacePresentation";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {ExpenseCategoryLabel} from "@/features/expenses/ExpenseCategoryLabel";
import {toCsv} from "@/features/late-penalties/model";
import {readReport,resolvePeriod,subtractMoney,validPeriod,YEAR_PERIOD,type PeriodChoice,type Report} from "./model";

const choices:{value:PeriodChoice;icon:LucideIcon}[]=[{value:"thisMonth",icon:CalendarDays},{value:"previousMonth",icon:CalendarMinus},{value:"thisYear",icon:CalendarRange},{value:"month",icon:CalendarSearch},{value:"year",icon:CalendarClock}];
function download(name:string,content:string){const url=URL.createObjectURL(new Blob([content],{type:"text/csv;charset=utf-8"}));const link=document.createElement("a");link.href=url;link.download=name;link.click();URL.revokeObjectURL(url);}
const monthName=(period:string,locale:Locale)=>new Intl.DateTimeFormat(locale==="en"?"en-GB":"tr-TR",{month:"long",year:"numeric",timeZone:"UTC"}).format(new Date(period+"-01T00:00:00Z"));
export const periodLabel=(period:string,locale:Locale)=>YEAR_PERIOD.test(period)?period:monthName(period,locale);

/** Quick and custom report periods; emits YYYY-MM, YYYY or "" while the custom value is invalid. */
export function ReportPeriodPicker({onChange}:{onChange:(period:string)=>void}){
 const t=useTranslations("reports");const today=businessToday();
 const [choice,setChoice]=useState<PeriodChoice>("thisMonth"),[month,setMonth]=useState(today.slice(0,7)),[year,setYear]=useState(today.slice(0,4));
 const period=resolvePeriod(choice,today,month,year);
 useEffect(()=>onChange(validPeriod(period)?period:""),[period,onChange]);
 return <section className="mb-6 rounded-2xl border bg-card p-5 print:hidden"><p className="mb-3 text-sm font-medium">{t("period")}</p>
  <FilterChips label={t("period")} value={choice} onChange={value=>setChoice(value as PeriodChoice)} items={choices.map(c=>({value:c.value,icon:c.icon,label:t("choice_"+c.value)}))}/>
  {choice==="month"&&<div className="mt-3"><Label htmlFor="report-month">{t("choice_month")}</Label><Input id="report-month" className="mt-2 w-48" type="month" min="2000-01" max="9998-12" value={month} onChange={e=>setMonth(e.target.value)}/></div>}
  {choice==="year"&&<div className="mt-3"><Label htmlFor="report-year">{t("choice_year")}</Label><Input id="report-year" className="mt-2 w-32" type="number" inputMode="numeric" min={2000} max={9998} value={year} onChange={e=>setYear(e.target.value)}/></div>}
  {!validPeriod(period)&&<p className="mt-3 text-sm text-destructive">{t("invalidPeriod")}</p>}
  <p className="mt-3 text-xs text-muted-foreground">{t("scopeHelp")}</p></section>;
}

/** Accrual, cash, open balance and expenses for one month or one calendar year, with CSV and print. */
export function FinanceSummary({propertyId,report,loading}:{propertyId:string;report:Report|null;loading:boolean}){
 const t=useTranslations("reports"),et=useTranslations("expenses"),locale=useLocale() as Locale;
 const amount=(v?:string)=>loading?"…":v?formatDecimalCurrency(v,locale):"—";const net=report?subtractMoney(report.receiptsAmount,report.expensesAmount):undefined;
 const exportCsv=()=>{if(!report)return;const rows:(string|number)[][]=[[t("period"),report.period],[t("sumAssessed"),report.assessedAmount],[t("sumReceipts"),report.receiptsAmount],[t("sumOutstanding"),report.outstandingAmount],[t("sumExpenses"),report.expensesAmount],[t("net"),net!]];
  if(report.months)rows.push([],[t("month"),t("sumAssessed"),t("sumReceipts"),t("sumOutstanding"),t("sumExpenses")],...report.months.map(m=>[m.period,m.assessedAmount,m.receiptsAmount,m.outstandingAmount,m.expensesAmount]));
  if(report.expenseCategories)rows.push([],[et("category"),t("sumExpenses")],...report.expenseCategories.map(c=>[et(c.category),c.amount]));
  download(`finans-rapor-${report.period}.csv`,toCsv(rows));};
 return <div className="mb-6 space-y-6">
  <div className="flex flex-wrap items-center justify-between gap-2"><h2 className="flex items-center gap-2 text-lg font-bold"><Scale aria-hidden="true" className="size-5 text-primary"/>{t("financeTitle")}{report&&<span className="text-muted-foreground">· {periodLabel(report.period,locale)}</span>}</h2>
   <div className="flex flex-wrap gap-2 print:hidden"><Button variant="outline" disabled={!report} onClick={exportCsv}><FileDown/>{t("exportCsv")}</Button><Button variant="outline" disabled={!report} onClick={()=>window.print()}><Printer/>{t("printPdf")}</Button></div></div>
  <section className="grid gap-3 sm:grid-cols-2 xl:grid-cols-5"><SummaryCard compact title={t("sumAssessed")} value={amount(report?.assessedAmount)} note={t("sumAssessedHelp")} icon={ReceiptText}/><SummaryCard compact title={t("sumReceipts")} value={amount(report?.receiptsAmount)} note={t("sumReceiptsHelp")} icon={WalletCards}/><SummaryCard compact title={t("sumOutstanding")} value={amount(report?.outstandingAmount)} note={t("sumOutstandingHelp")} icon={ReceiptText}/><SummaryCard compact title={t("sumExpenses")} value={amount(report?.expensesAmount)} note={t("sumExpensesHelp")} icon={ArrowUpRight}/><SummaryCard compact title={t("net")} value={amount(net)} note={t("netHelp")} icon={Scale}/></section>
  {report?.months&&<section className="overflow-x-auto rounded-2xl border bg-card p-5 shadow-sm"><h3 className="mb-3 flex items-center gap-2 font-semibold"><Table2 aria-hidden="true" className="size-5 text-primary"/>{t("monthlyBreakdown")}</h3>
   <table className="w-full text-left text-sm"><thead><tr className="text-muted-foreground"><th className="py-2 pr-3">{t("month")}</th><th className="pr-3 text-right">{t("sumAssessed")}</th><th className="pr-3 text-right">{t("sumReceipts")}</th><th className="pr-3 text-right">{t("sumOutstanding")}</th><th className="pr-3 text-right">{t("sumExpenses")}</th><th className="text-right">{t("net")}</th></tr></thead>
    <tbody>{report.months.map(m=><tr key={m.period} className="border-t"><td className="py-2 pr-3 font-medium">{monthName(m.period,locale)}</td><td className="pr-3 text-right">{formatDecimalCurrency(m.assessedAmount,locale)}</td><td className="pr-3 text-right">{formatDecimalCurrency(m.receiptsAmount,locale)}</td><td className="pr-3 text-right">{formatDecimalCurrency(m.outstandingAmount,locale)}</td><td className="pr-3 text-right">{formatDecimalCurrency(m.expensesAmount,locale)}</td><td className="text-right">{formatDecimalCurrency(subtractMoney(m.receiptsAmount,m.expensesAmount),locale)}</td></tr>)}
     <tr className="border-t font-semibold"><td className="py-2 pr-3">{t("total")}</td><td className="pr-3 text-right">{formatDecimalCurrency(report.assessedAmount,locale)}</td><td className="pr-3 text-right">{formatDecimalCurrency(report.receiptsAmount,locale)}</td><td className="pr-3 text-right">{formatDecimalCurrency(report.outstandingAmount,locale)}</td><td className="pr-3 text-right">{formatDecimalCurrency(report.expensesAmount,locale)}</td><td className="text-right">{formatDecimalCurrency(net!,locale)}</td></tr></tbody></table></section>}
  {report?.expenseCategories&&<section className="overflow-x-auto rounded-2xl border bg-card p-5 shadow-sm"><h3 className="mb-3 flex items-center gap-2 font-semibold"><ArrowUpRight aria-hidden="true" className="size-5 text-primary"/>{t("expenseBreakdown")}</h3>
   <table className="w-full text-left text-sm"><thead><tr className="text-muted-foreground"><th className="py-2 pr-3">{et("category")}</th><th className="text-right">{t("sumExpenses")}</th></tr></thead>
    <tbody>{report.expenseCategories.map(c=><tr key={c.category} className="border-t"><td className="py-2 pr-3"><ExpenseCategoryLabel propertyId={propertyId} category={c.category} label={et(c.category)}/></td><td className="text-right">{formatDecimalCurrency(c.amount,locale)}</td></tr>)}</tbody></table></section>}
  <p className="text-xs text-muted-foreground">{t("financeBasis")}</p>
 </div>;
}

/** Self-contained finance report for the dues "Rapor" tab. */
export function FinanceReport({propertyId}:{propertyId:string}){
 const locale=useLocale() as Locale,api=useAuthorizedApi(),ui=useTranslations("ui");
 const [period,setPeriod]=useState(""),[report,setReport]=useState<Report|null>(null),[loading,setLoading]=useState(false),[error,setError]=useState(""),[revision,setRevision]=useState(0);
 useEffect(()=>{const c=new AbortController();setReport(null);setError("");setLoading(!!period);if(period)void api<unknown>(`/api/v1/properties/${propertyId}/reports?period=${period}`,{signal:c.signal}).then(v=>{if(!c.signal.aborted)setReport(readReport(v,propertyId,period));}).catch(e=>{if(!c.signal.aborted)setError(errorMessage(e,locale));}).finally(()=>{if(!c.signal.aborted)setLoading(false);});return()=>c.abort();},[api,propertyId,period,locale,revision]);
 return <><ReportPeriodPicker onChange={setPeriod}/>{error&&<MessageBanner action={<Button variant="view" onClick={()=>setRevision(n=>n+1)}>{ui("tryAgain")}</Button>}>{error}</MessageBanner>}<FinanceSummary propertyId={propertyId} report={report} loading={loading}/></>;
}
