import {cn} from "@/lib/utils";
import {statusTone,typeStyle} from "./type-style";
import type {MaintenanceStatus,MaintenanceType as Type} from "./model";
export function MaintenanceType({type,label}:{type:Type;label:string}){const c=typeStyle[type];return <span className="request-type"><span className={`request-type-icon tone-${c.tone}`}><c.icon aria-hidden="true"/></span>{label}</span>;}
export function MaintenanceStatusBadge({status,label}:{status:MaintenanceStatus;label:string}){return <span className={cn("dashboard-status",`tone-${statusTone[status]}`)}>{label}</span>;}
