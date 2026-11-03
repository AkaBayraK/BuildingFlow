import {ManagementPage} from "@/features/navigation/ManagementPage";
import {readUnitListFilter} from "@/features/hierarchy/occupancy-filter";
export default async function Page({params,searchParams}:{params:Promise<{id:string;buildingId:string}>;searchParams:Promise<{unitId?:string|string[];occupancy?:string|string[]}>}){const {id,buildingId}=await params;const query=await searchParams;return <ManagementPage section="units" buildingId={buildingId} unitFilter={readUnitListFilter(query.unitId,query.occupancy)}/>;}
