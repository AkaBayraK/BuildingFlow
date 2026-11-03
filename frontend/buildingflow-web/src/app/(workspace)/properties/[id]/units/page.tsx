import {ManagementPage} from "@/features/navigation/ManagementPage";
import {readUnitListFilter} from "@/features/hierarchy/occupancy-filter";
export default async function Page({params,searchParams}:{params:Promise<{id:string}>;searchParams:Promise<{unitId?:string|string[];occupancy?:string|string[]}>}){const {id}=await params;const query=await searchParams;return <ManagementPage section="units" unitFilter={readUnitListFilter(query.unitId,query.occupancy)}/>;}
