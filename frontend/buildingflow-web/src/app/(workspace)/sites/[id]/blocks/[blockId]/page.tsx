import { redirect } from "next/navigation";
export default async function Page({params}:{params:Promise<{id:string;blockId:string}>}){const {id,blockId}=await params;redirect(`/properties/${id}/buildings/${blockId}`);}
