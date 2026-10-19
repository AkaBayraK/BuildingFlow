import {notFound,redirect} from "next/navigation";
import {CenterDetail} from "@/features/person-center/CenterDetail";
const valid=(v:string)=>/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(v)&&v!=="00000000-0000-0000-0000-000000000000";
export default async function Page({params}:{params:Promise<{tenantId:string;personId:string}>}){if(process.env.BUILDINGFLOW_PERSON_CENTER_ENABLED!=="true"&&process.env.BUILDINGFLOW_SESSION_CONTEXTS_ENABLED!=="true")redirect("/people");const p=await params;if(!valid(p.tenantId)||!valid(p.personId))notFound();return <CenterDetail tenant={p.tenantId.toLowerCase()} person={p.personId.toLowerCase()}/>;}
