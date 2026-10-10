import {ManagementPage} from "@/features/navigation/ManagementPage";
export default async function Page({params}:{params:Promise<{id:string;buildingId:string}>}){const {id,buildingId}=await params;return <ManagementPage section="units" buildingId={buildingId}/>;}
