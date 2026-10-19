"use client";
import {MessageBanner} from "@/components/feedback/MessageBanner";
import {useLocale,useTranslations} from "next-intl";
import type {ReactNode} from "react";
import {flexRender,getCoreRowModel,useReactTable,type ColumnDef,type SortingState,type OnChangeFn,type VisibilityState} from "@tanstack/react-table";
import {ChevronLeft,ChevronRight} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Skeleton} from "@/components/ui/skeleton";
import {Table,TableHeader,TableHead,TableBody,TableCell,TableRow} from "@/components/ui/table";
import type {PagedRows} from "@/services/api";

export function RecordList<T extends {id:string}>({data,columns,loading,error,page,onPage,onRetry,empty,card,compact=false,sorting,onSortingChange,footerControls,columnVisibility,onColumnVisibilityChange,cardBreakpoint="md"}: {
  data:PagedRows<T>|null;columns:ColumnDef<T>[];loading:boolean;error:string;page:number;onPage:(page:number)=>void;onRetry:()=>void;empty:ReactNode;card:(value:T)=>ReactNode;
  cardBreakpoint?:"md"|"xl";columnVisibility?:VisibilityState;onColumnVisibilityChange?:OnChangeFn<VisibilityState>;compact?:boolean;sorting?:SortingState;onSortingChange?:OnChangeFn<SortingState>;footerControls?:ReactNode;
}) {
  const t=useTranslations("ui"); const locale=useLocale();

  const table=useReactTable({data:data?.items ?? [],columns,getCoreRowModel:getCoreRowModel(),getRowId:row=>row.id,manualSorting:true,enableSortingRemoval:false,enableMultiSort:false,state:{sorting:sorting??[],columnVisibility:columnVisibility??{}},onColumnVisibilityChange,onSortingChange});
  const pages=Math.max(1,Math.ceil((data?.totalCount ?? 0)/(data?.pageSize ?? 10)));
  return <>
    {loading?<div role="status" className="space-y-4 p-6"><span className="sr-only">{t("loading")}</span>{[0,1,2].map(i=><Skeleton key={i} className="h-16 w-full"/>)}</div>:error?<div className="p-8 text-center"><MessageBanner className="my-4 text-left">{error}</MessageBanner><Button variant="view" onClick={onRetry}>{t("tryAgain")}</Button></div>:!data?.items.length?<div className="p-10 text-center text-sm text-muted-foreground">{empty}</div>:<>
      <div className={cardBreakpoint==="xl"?"hidden xl:block":"hidden md:block"}><Table><TableHeader className="bg-muted/60">{table.getHeaderGroups().map(group=><TableRow key={group.id}>{group.headers.map(header=><TableHead key={header.id} className="px-5 text-xs" aria-sort={header.column.getIsSorted()==="asc"?"ascending":header.column.getIsSorted()==="desc"?"descending":undefined}>{flexRender(header.column.columnDef.header,header.getContext())}</TableHead>)}</TableRow>)}</TableHeader><TableBody>{table.getRowModel().rows.map(row=><TableRow key={row.id} className="hover:bg-secondary/40">{row.getVisibleCells().map(cell=><TableCell key={cell.id} className={compact?"px-5 py-3 whitespace-normal":"px-6 py-5 whitespace-normal"}>{flexRender(cell.column.columnDef.cell,cell.getContext())}</TableCell>)}</TableRow>)}</TableBody></Table></div>
      <div className={cardBreakpoint==="xl"?(data.items.length>1?"grid gap-3 p-4 sm:grid-cols-2 xl:hidden":"grid gap-3 p-4 xl:hidden"):"grid gap-3 p-4 md:hidden"}>{data.items.map(row=><article key={row.id} className="min-h-0 rounded-xl border bg-card p-4 [&_p]:min-h-0">{card(row)}</article>)}</div>
    </>}
    <div className="flex flex-wrap items-center justify-between gap-3 border-t p-4"><span className="text-xs text-muted-foreground">{error?t("couldNotLoadTheList"):loading?t("loading"):t("recordCount",{count:data?.totalCount ?? 0})}</span><div className="flex flex-wrap items-center gap-3">{footerControls}<span className="text-xs text-muted-foreground">{page} / {pages}</span><Button variant="outline" size="icon-sm" aria-label={t("previousPage")} disabled={loading || page<=1} onClick={()=>onPage(page-1)}><ChevronLeft/></Button><Button variant="outline" size="icon-sm" aria-label={t("nextPage")} disabled={loading || !!error || page>=pages} onClick={()=>onPage(page+1)}><ChevronRight/></Button></div></div>
  </>;
}
