import {notFound} from "next/navigation";
import {DuesWorkspace} from "@/features/dues/DuesWorkspace";
export default async function Page({params}:{params:Promise<{id:string}>}){const {id}=await params;if(!/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(id))notFound();return <DuesWorkspace key={id} propertyId={id}/>;}
