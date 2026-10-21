import {cn} from "@/lib/utils";
import {categoryStyle} from "./category-style";
import type {AnnouncementCategory as Category} from "./model";
/** Small coloured category pill used in the announcement list, cards and detail panel. */
export function AnnouncementCategoryBadge({category,label}:{category:Category;label:string}){const c=categoryStyle[category];return <span className={cn("announcement-category",`tone-${c.tone}`)}><c.icon aria-hidden="true"/>{label}</span>;}
export function AnnouncementCategoryIcon({category,label}:{category:Category;label:string}){const c=categoryStyle[category];return <span className={cn("dashboard-icon",`tone-${c.tone}`)} title={label}><c.icon aria-hidden="true"/><span className="sr-only">{label}</span></span>;}
