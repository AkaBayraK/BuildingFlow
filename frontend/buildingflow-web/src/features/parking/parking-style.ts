// Araçlar/Otopark icons: one icon and tone per tab, space type, vehicle and guest status (ikon zorunlu kuralı).
import {BarChart3,Wrench,Accessibility,BadgeCheck,Ban,Bike,CalendarClock,Car,CarFront,CircleParking,CircleSlash,Clock3,DoorOpen,Ellipsis,LayoutDashboard,LogIn,LogOut,Map as MapIcon,Settings2,Truck,UserRoundCheck,Users,CircleX,Zap,type LucideIcon} from "lucide-react";
import type {Tone} from "@/features/requests/category-style";
import type {GuestStatus,SpaceType,VehicleStatus,VehicleType} from "./model";
export const parkingTabs=["overview","vehicles","spaces","allocations","guests","closures","reports","rules"] as const;
export type ParkingTab=typeof parkingTabs[number];
export const tabIcon:Record<ParkingTab,LucideIcon>={overview:LayoutDashboard,vehicles:Car,spaces:MapIcon,allocations:DoorOpen,guests:Users,closures:Wrench,reports:BarChart3,rules:Settings2};
export const spaceStyle:Record<SpaceType,{icon:LucideIcon;tone:Tone}>={STANDARD:{icon:CircleParking,tone:"blue"},ACCESSIBLE:{icon:Accessibility,tone:"violet"},VISITOR:{icon:UserRoundCheck,tone:"amber"},EV_CHARGING:{icon:Zap,tone:"teal"},MOTORCYCLE:{icon:Bike,tone:"neutral"}};
export const vehicleTypeIcon:Record<VehicleType,LucideIcon>={CAR:CarFront,MOTORCYCLE:Bike,VAN:Truck,OTHER:Ellipsis};
export const vehicleStatusStyle:Record<VehicleStatus,{icon:LucideIcon;tone:Tone}>={PENDING:{icon:Clock3,tone:"amber"},APPROVED:{icon:BadgeCheck,tone:"green"},REJECTED:{icon:CircleX,tone:"red"},PASSIVE:{icon:CircleSlash,tone:"neutral"}};
export const guestStatusStyle:Record<GuestStatus,{icon:LucideIcon;tone:Tone}>={PLANNED:{icon:CalendarClock,tone:"blue"},ARRIVED:{icon:LogIn,tone:"green"},DEPARTED:{icon:LogOut,tone:"neutral"},CANCELLED:{icon:Ban,tone:"red"}};
/** Map legend: allocated, visitor, accessible, free. Colour is never the only signal; each tile also shows an icon and text. */
export const mapState={closed:{icon:Wrench,tone:"amber"},allocated:{icon:Car,tone:"red"},visitor:{icon:UserRoundCheck,tone:"amber"},accessible:{icon:Accessibility,tone:"violet"},free:{icon:CircleParking,tone:"green"}} as const satisfies Record<string,{icon:LucideIcon;tone:Tone}>;
