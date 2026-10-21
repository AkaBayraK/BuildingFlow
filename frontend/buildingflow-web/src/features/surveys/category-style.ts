// Survey category icons and colours: one icon per subject, like requests/category-style.ts and meters/kind-style.ts.
// Chart colours are a validated categorical order (dataviz validator, light surface): identity never relies on colour alone,
// every segment also has its icon and label in the legend.
import {CalendarClock,CircleCheckBig,CirclePlay,Coins,ConciergeBell,FilePen,MessagesSquare,PartyPopper,ShieldCheck,SquareParking,TimerOff,Trees,type LucideIcon} from "lucide-react";
import type {Tone} from "@/features/requests/category-style";
import type {SurveyCategory,SurveyState} from "./model";
export const categoryStyle:Record<SurveyCategory,{icon:LucideIcon;color:string}>={
 DUES:{icon:Coins,color:"#2a78d6"},SERVICE:{icon:ConciergeBell,color:"#eb6834"},SECURITY:{icon:ShieldCheck,color:"#1baf7a"},SOCIAL:{icon:PartyPopper,color:"#eda100"},
 PARKING:{icon:SquareParking,color:"#e87ba4"},ENVIRONMENT:{icon:Trees,color:"#008300"},GENERAL:{icon:MessagesSquare,color:"#6250d6"},
};
/** State badge colours: draft neutral, scheduled violet, active green, expired amber, completed blue. */
export const stateTone={DRAFT:"neutral",SCHEDULED:"violet",ACTIVE:"green",EXPIRED:"amber",COMPLETED:"blue"} as const satisfies Record<SurveyState,Tone>;
/** State icons for tabs and badges, so a state is never told apart by colour alone. */
export const stateIcon:Record<SurveyState,LucideIcon>={DRAFT:FilePen,SCHEDULED:CalendarClock,ACTIVE:CirclePlay,EXPIRED:TimerOff,COMPLETED:CircleCheckBig};
