import {ManagementPage} from "@/features/navigation/ManagementPage";
import {readUnitFilter} from "@/features/people/unit-links";
export default async function Page({params,searchParams}:{params:Promise<{id:string}>;searchParams:Promise<{unitId?:string|string[]}>}){const {id}=await params;const query=await searchParams;return <ManagementPage section="units" unitFilter={readUnitFilter(query.unitId)}/>;}
