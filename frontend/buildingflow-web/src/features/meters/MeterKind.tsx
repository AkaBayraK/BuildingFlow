import {kindStyle} from "./kind-style";
import type {Meter} from "./model";
export function MeterKind({kind,label}:{kind:Meter["kind"];label:string}){const c=kindStyle[kind];return <span className="request-type"><span className={`request-type-icon tone-${c.tone}`}><c.icon aria-hidden="true"/></span>{label}</span>;}
