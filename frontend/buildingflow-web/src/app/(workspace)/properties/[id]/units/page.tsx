import {ManagementPage} from "@/features/navigation/ManagementPage";
export default async function Page({params}:{params:Promise<{id:string}>}){const {id}=await params;return <ManagementPage section="units"/>;}
