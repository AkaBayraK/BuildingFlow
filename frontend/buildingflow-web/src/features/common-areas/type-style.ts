import {Wrench,CircleCheck,CircleDot,Ban,CalendarCheck,CalendarX2,Waves,Dumbbell,Presentation,Baby,Flame,Trophy,PartyPopper,Shapes,CheckCircle2,PauseCircle,Building2,DoorOpen,CalendarCheck2,ShieldCheck,Footprints,type LucideIcon} from 'lucide-react';
import type {Tone} from '@/features/requests/category-style';
import type {AreaType,Area,Segment,Reservation} from './model';
export const typeStyle:Record<AreaType,{icon:LucideIcon;tone:Tone}>={POOL:{icon:Waves,tone:'blue'},FITNESS:{icon:Dumbbell,tone:'violet'},MEETING_ROOM:{icon:Presentation,tone:'teal'},PLAYGROUND:{icon:Baby,tone:'amber'},BARBECUE:{icon:Flame,tone:'red'},SPORTS_COURT:{icon:Trophy,tone:'green'},EVENT_HALL:{icon:PartyPopper,tone:'violet'},OTHER:{icon:Shapes,tone:'neutral'}};
export const statusStyle={active:{icon:CheckCircle2,tone:'green'},inactive:{icon:PauseCircle,tone:'neutral'}} as const;
export const visibilityStyle:Record<Area['visibility'],LucideIcon>={PROPERTY:Building2,BUILDING:DoorOpen};
export const usageStyle:Record<Area['usage'],LucideIcon>={OPEN_USE:Footprints,RESERVATION:CalendarCheck2,APPROVAL:ShieldCheck};

export const badgeTone={blue:'info',green:'success',amber:'warning',red:'error',violet:'violet',teal:'teal',neutral:'neutral'} as const;
export const segmentStyle:Record<Segment['state'],{icon:LucideIcon;tone:'success'|'warning'|'error'}>={FREE:{icon:CircleCheck,tone:'success'},PARTIAL:{icon:CircleDot,tone:'warning'},FULL:{icon:Ban,tone:'error'},CLOSED:{icon:Wrench,tone:'warning'}};
export const reservationStyle:Record<Reservation['status'],{icon:LucideIcon;tone:'success'|'neutral'}>={CONFIRMED:{icon:CalendarCheck,tone:'success'},CANCELLED:{icon:CalendarX2,tone:'neutral'}};

export const calendarFilterStyle={'':CalendarCheck2,CONFIRMED:CalendarCheck,CANCELLED:CalendarX2} as const;
