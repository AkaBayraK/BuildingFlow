import type {LucideIcon} from "lucide-react";
import type {Tone} from "@/features/requests/category-style";
import {cn} from "@/lib/utils";
export function ParkingBadge({icon:Icon,tone,label}:{icon:LucideIcon;tone:Tone;label:string}){return <span className={cn("dashboard-status inline-flex items-center gap-1",`tone-${tone}`)}><Icon aria-hidden="true" className="size-3.5"/>{label}</span>;}
export function ParkingKind({icon:Icon,tone,label}:{icon:LucideIcon;tone:Tone;label:string}){return <span className="request-type"><span className={`request-type-icon tone-${tone}`}><Icon aria-hidden="true"/></span>{label}</span>;}
