// Kişiye özel dashboard düzeni (TraceFlow widget ızgarasıyla aynı davranış): sıra, genişlik ve gizli kartlar
// hesap bazında tarayıcıda saklanır. Kayıt yoksa ya da bozuksa varsayılan düzen kullanılır.
export const WIDGET_IDS=["kpis","trend","occupancy","recent","collection","upcoming"] as const;
export type WidgetId=typeof WIDGET_IDS[number];
/** Geniş ekranda üç sütunlu ızgarada kaç sütun kaplanacağı; telefonda her kart tam genişliktir. */
export const WIDGET_SIZES=[1,2,3] as const;
export type WidgetSize=typeof WIDGET_SIZES[number];
export type WidgetLayout={id:WidgetId;size:WidgetSize;hidden:boolean};
/** Mevcut sabit dizilimin karşılığı: trend 2/3 + doluluk 1/3, talepler tam satır, tahsilat 1/3 + yaklaşanlar 2/3. */
export const DEFAULT_LAYOUT:readonly WidgetLayout[]=[
 {id:"kpis",size:3,hidden:false},{id:"trend",size:2,hidden:false},{id:"occupancy",size:1,hidden:false},
 {id:"recent",size:3,hidden:false},{id:"collection",size:1,hidden:false},{id:"upcoming",size:2,hidden:false}];
const defaults=()=>DEFAULT_LAYOUT.map(w=>({...w}));
const isId=(v:unknown):v is WidgetId=>WIDGET_IDS.includes(v as WidgetId);
const isSize=(v:unknown):v is WidgetSize=>WIDGET_SIZES.includes(v as WidgetSize);
/** Kayıtlı düzeni varsayılanla birleştirir: bilinmeyen/yinelenen kayıt düşer, sonradan eklenen kart varsayılan yerine/boyutuyla sona gelir. */
export function mergeLayout(saved:unknown):WidgetLayout[]{
 if(!Array.isArray(saved))return defaults();
 const seen=new Set<WidgetId>(),known:WidgetLayout[]=[];
 for(const item of saved){
  if(!item||typeof item!=="object")continue;const {id,size,hidden}=item as Record<string,unknown>;
  if(!isId(id)||seen.has(id))continue;seen.add(id);
  const base=DEFAULT_LAYOUT.find(w=>w.id===id)!;known.push({id,size:isSize(size)?size:base.size,hidden:hidden===true});
 }
 return [...known,...defaults().filter(w=>!seen.has(w.id))];
}
export const isDefaultLayout=(layout:readonly WidgetLayout[])=>layout.length===DEFAULT_LAYOUT.length&&layout.every((w,i)=>w.id===DEFAULT_LAYOUT[i].id&&w.size===DEFAULT_LAYOUT[i].size&&w.hidden===DEFAULT_LAYOUT[i].hidden);
/** Sürükle-bırak: ileri taşımada kart hedefin yerine geçer, geri taşımada hedefin önüne girer. */
export function dropWidget(layout:readonly WidgetLayout[],source:WidgetId,target:WidgetId):WidgetLayout[]{
 const from=layout.findIndex(w=>w.id===source),to=layout.findIndex(w=>w.id===target);
 if(from<0||to<0||from===to)return [...layout];
 const next=layout.filter(w=>w.id!==source),at=next.findIndex(w=>w.id===target);
 next.splice(from<to?at+1:at,0,layout[from]);return next;
}
/** Klavye/dokunmatik taşıma: görünür kartlar arasında bir adım; gizli kartlar atlanır. */
export function shiftWidget(layout:readonly WidgetLayout[],id:WidgetId,delta:-1|1,visible:readonly WidgetId[]):WidgetLayout[]{
 const order=visible.filter(v=>layout.some(w=>w.id===v&&!w.hidden)),i=order.indexOf(id),neighbour=order[i+delta];
 return i<0||!neighbour?[...layout]:dropWidget(layout,id,neighbour);
}
export const resizeWidget=(layout:readonly WidgetLayout[],id:WidgetId,size:WidgetSize)=>layout.map(w=>w.id===id?{...w,size}:w);
export const setWidgetHidden=(layout:readonly WidgetLayout[],id:WidgetId,hidden:boolean)=>layout.map(w=>w.id===id?{...w,hidden}:w);
const key=(subject:string)=>`buildingflow.dashboard-layout.${subject}`;
// Depolama gizli sekmede/kapalıyken hata atar; düzen kritik veri değildir, okunamazsa varsayılanla devam edilir.
export function readLayout(storage:Pick<Storage,"getItem">,subject:string):WidgetLayout[]{
 try{const raw=storage.getItem(key(subject));return mergeLayout(raw?JSON.parse(raw):null);}catch{return defaults();}
}
export function writeLayout(storage:Pick<Storage,"setItem">,subject:string,layout:readonly WidgetLayout[]):boolean{
 try{storage.setItem(key(subject),JSON.stringify(layout));return true;}catch{return false;}
}
export function clearLayout(storage:Pick<Storage,"removeItem">,subject:string):void{
 try{storage.removeItem(key(subject));}catch{/* Silinemezse varsayılana dönüş yine ekranda uygulanır. */}
}
