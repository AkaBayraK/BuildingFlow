// Announcement category icons: same icon and colour language as the request types (requests/category-style.ts).
import {Ellipsis,Megaphone,TriangleAlert,Users,Wallet,Wrench,type LucideIcon} from "lucide-react";
import type {Tone} from "@/features/requests/category-style";
import type {AnnouncementCategory} from "./model";
export const categoryStyle:Record<AnnouncementCategory,{icon:LucideIcon;tone:Tone}>={
 GENERAL:{icon:Megaphone,tone:"blue"},MEETING:{icon:Users,tone:"violet"},DUES:{icon:Wallet,tone:"amber"},MAINTENANCE:{icon:Wrench,tone:"teal"},URGENT:{icon:TriangleAlert,tone:"red"},OTHER:{icon:Ellipsis,tone:"neutral"},
};
