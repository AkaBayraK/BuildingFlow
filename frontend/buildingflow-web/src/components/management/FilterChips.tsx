import type {LucideIcon} from "lucide-react";
import {cn} from "@/lib/utils";
import type {Tone} from "@/features/requests/category-style";
/** Every chip carries an icon (AGENTS.md UI rule): tabs, filters, types and categories are never text-only. */
export type FilterChip={value:string;label:string;count?:number;icon:LucideIcon;tone?:Tone};
/** Single-choice chip row (type/category/state filters); counts come from the server, never from the visible page. */
export function FilterChips({label,items,value,onChange,disabled}:{label:string;items:readonly FilterChip[];value:string;onChange:(value:string)=>void;disabled?:boolean}){
 return <div role="group" aria-label={label} className="filter-chips">{items.map(item=>{const active=item.value===value;return <button key={item.value} type="button" aria-pressed={active} disabled={disabled} onClick={()=>onChange(item.value)} className={cn("filter-chip",active&&"filter-chip-active",!active&&item.tone&&`filter-chip-${item.tone}`)}>
  <item.icon aria-hidden="true"/><span>{item.label}</span>{item.count!==undefined&&<span className="filter-chip-count">({item.count})</span>}
 </button>;})}</div>;
}
