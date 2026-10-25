import {CircleCheck,Clock3,MailQuestion,ShieldAlert,TriangleAlert} from "lucide-react";
import type {Tone} from "@/components/management/WorkspacePresentation";
import type {ContactState} from "./contact-verification";
export const contactStyle={VERIFIED:{tone:"success",icon:CircleCheck},PENDING:{tone:"warning",icon:Clock3},UNVERIFIED:{tone:"warning",icon:ShieldAlert},MISSING:{tone:"info",icon:MailQuestion},UNKNOWN:{tone:"error",icon:TriangleAlert}} satisfies Record<ContactState,{tone:Tone;icon:typeof CircleCheck}>;
