"use client";
import {useEffect,useState,type DragEvent,type ReactNode} from "react";
import {useTranslations} from "next-intl";
import {Check,ChevronLeft,ChevronRight,Eye,EyeOff,GripVertical,LayoutDashboard,Maximize2,RotateCcw} from "lucide-react";
import {Button} from "@/components/ui/button";
import {Dialog,DialogContent,DialogDescription,DialogHeader,DialogTitle} from "@/components/ui/dialog";
import {cn} from "@/lib/utils";
import {WIDGET_SIZES,clearLayout,mergeLayout,dropWidget,isDefaultLayout,readLayout,resizeWidget,setWidgetHidden,shiftWidget,writeLayout,type WidgetId,type WidgetLayout,type WidgetSize} from "./dashboard-layout";

export type DashboardWidget={id:WidgetId;title:string;node:ReactNode};

/**
 * Düzenlenebilir kart ızgarası (TraceFlow dashboard'u örnek alındı). Sürükleme yalnız tutamaçtan başlar:
 * kart gövdesi grafik, seçim ve bağlantılarla dolu, oradan sürükleme kazayla tetiklenirdi.
 * Düzen hesap bazında tarayıcıda saklanır; yazılamazsa bu oturumda geçerli kalır. Sunucu çiziminde
 * depolama yoktur, kayıt ilk efektte okunur (hidrasyon uyuşmazlığı olmaz).
 */
export function DashboardWidgets({subject,widgets}:{subject:string;widgets:DashboardWidget[]}){
 const t=useTranslations("dashboard");
 const [layout,setLayout]=useState<WidgetLayout[]>(()=>mergeLayout(null));
 const [editing,setEditing]=useState(false),[grabbed,setGrabbed]=useState<WidgetId|null>(null),[dragging,setDragging]=useState<WidgetId|null>(null),[target,setTarget]=useState<WidgetId|null>(null),[expanded,setExpanded]=useState<WidgetId|null>(null);
 useEffect(()=>{try{setLayout(readLayout(window.localStorage,subject));}catch{}},[subject]);
 const available=new Map(widgets.map(w=>[w.id,w]));
 // Bu alanda verisi olmayan kart (ör. muhasebecide talepler) düzende yerini korur, yalnız çizilmez.
 const shown=layout.filter(w=>available.has(w.id)&&!w.hidden),hidden=layout.filter(w=>available.has(w.id)&&w.hidden),visibleIds=shown.map(w=>w.id);
 function apply(next:WidgetLayout[]){setLayout(next);try{writeLayout(window.localStorage,subject,next);}catch{}}
 function reset(){try{clearLayout(window.localStorage,subject);}catch{}setLayout(mergeLayout(null));}
 const endDrag=()=>{setGrabbed(null);setDragging(null);setTarget(null);};
 function over(e:DragEvent<HTMLDivElement>,id:WidgetId){if(!dragging||dragging===id)return;e.preventDefault();e.dataTransfer.dropEffect="move";setTarget(id);}
 const open=expanded?available.get(expanded):undefined;
 return <>
  <div className="dashboard-layout-bar" role="toolbar" aria-label={t("layoutToolbar")}>
   {editing&&<p className="dashboard-layout-hint">{t("layoutHint")}</p>}
   {!isDefaultLayout(layout)&&<Button variant="outline" size="sm" onClick={reset}><RotateCcw/>{t("layoutReset")}</Button>}
   {editing?<Button size="sm" onClick={()=>{setEditing(false);endDrag();}}><Check/>{t("layoutDone")}</Button>:<Button variant="edit" size="sm" onClick={()=>setEditing(true)}><LayoutDashboard/>{t("layoutEdit")}</Button>}
  </div>
  {editing&&hidden.length>0&&<div className="dashboard-hidden-widgets"><span>{t("layoutHidden")}</span>{hidden.map(w=><Button key={w.id} variant="view" size="sm" onClick={()=>apply(setWidgetHidden(layout,w.id,false))}><Eye/>{available.get(w.id)!.title}</Button>)}</div>}
  <div className={cn("dashboard-widgets",editing&&"is-editing")}>
   {shown.map((w,index)=>{const {title,node}=available.get(w.id)!;return <div key={w.id} className={cn("dashboard-widget",`span-${w.size}`,dragging===w.id&&"is-dragging",target===w.id&&"is-target")} draggable={editing&&grabbed===w.id}
    onDragStart={e=>{e.dataTransfer.effectAllowed="move";e.dataTransfer.setData("text/plain",w.id);setDragging(w.id);}} onDragEnd={endDrag}
    onDragOver={e=>over(e,w.id)} onDragLeave={()=>setTarget(c=>c===w.id?null:c)} onDrop={e=>{e.preventDefault();const source=e.dataTransfer.getData("text/plain") as WidgetId;endDrag();if(source)apply(dropWidget(layout,source,w.id));}}>
    <div className="dashboard-widget-tools" role="group" aria-label={t("widgetTools",{title})}>
     {editing&&<>
      <button type="button" className="dashboard-widget-grip" aria-label={t("widgetMove",{title})} title={t("widgetMoveHint")} onPointerDown={()=>setGrabbed(w.id)} onPointerUp={()=>setGrabbed(null)}
       onKeyDown={e=>{const delta=e.key==="ArrowRight"||e.key==="ArrowDown"?1:e.key==="ArrowLeft"||e.key==="ArrowUp"?-1:0;if(delta){e.preventDefault();apply(shiftWidget(layout,w.id,delta,visibleIds));}}}><GripVertical aria-hidden="true"/></button>
      <button type="button" disabled={index===0} onClick={()=>apply(shiftWidget(layout,w.id,-1,visibleIds))} aria-label={t("widgetEarlier",{title})} title={t("widgetEarlier",{title})}><ChevronLeft aria-hidden="true"/></button>
      <button type="button" disabled={index===shown.length-1} onClick={()=>apply(shiftWidget(layout,w.id,1,visibleIds))} aria-label={t("widgetLater",{title})} title={t("widgetLater",{title})}><ChevronRight aria-hidden="true"/></button>
      <span className="dashboard-widget-sizes" role="radiogroup" aria-label={t("widgetSize",{title})}>{WIDGET_SIZES.map((size:WidgetSize)=><button key={size} type="button" role="radio" aria-checked={w.size===size} className={w.size===size?"is-active":""} onClick={()=>apply(resizeWidget(layout,w.id,size))} title={t(`widgetSize${size}`)}>{t(`widgetSizeShort${size}`)}</button>)}</span>
      <button type="button" onClick={()=>apply(setWidgetHidden(layout,w.id,true))} aria-label={t("widgetHide",{title})} title={t("widgetHide",{title})}><EyeOff aria-hidden="true"/></button>
     </>}
     <button type="button" onClick={()=>setExpanded(w.id)} aria-label={t("widgetExpand",{title})} title={t("widgetExpand",{title})}><Maximize2 aria-hidden="true"/></button>
    </div>
    {/* Büyütme penceresi açıkken aynı kart iki kez çizilmez; kimlikli başlıklar tek kalır. */}
    {expanded===w.id?<div className="dashboard-widget-placeholder" aria-hidden="true"/>:node}
   </div>;})}
  </div>
  <Dialog open={!!open} onOpenChange={value=>{if(!value)setExpanded(null);}}>
   <DialogContent className="dashboard-widget-dialog max-h-[90vh] overflow-y-auto sm:max-w-5xl"><DialogHeader><DialogTitle>{open?.title}</DialogTitle><DialogDescription className="sr-only">{t("widgetExpandedNote")}</DialogDescription></DialogHeader>{open?.node}</DialogContent>
  </Dialog>
 </>;
}
