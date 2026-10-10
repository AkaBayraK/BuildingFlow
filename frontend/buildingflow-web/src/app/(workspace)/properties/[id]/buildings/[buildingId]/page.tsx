import { BuildingDetail } from "@/features/hierarchy/BuildingDetail";
export default async function Page({params}:{params:Promise<{id:string;buildingId:string}>}){const {id,buildingId}=await params;return <BuildingDetail propertyId={id} buildingId={buildingId}/>;}
