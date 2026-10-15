import {ManagementPage} from "@/features/navigation/ManagementPage";
import {readUnitFilter} from "@/features/people/unit-links";
export default async function Page({params,searchParams}:{params:Promise<{id:string;buildingId:string}>;searchParams:Promise<{unitId?:string|string[]}>}){const {id,buildingId}=await params;const query=await searchParams;return <ManagementPage section="units" buildingId={buildingId} unitFilter={readUnitFilter(query.unitId)}/>;}
