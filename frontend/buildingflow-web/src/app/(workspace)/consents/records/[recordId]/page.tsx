import {notFound} from "next/navigation";
import {ConsentDocumentPage} from "@/features/consents/ConsentDocumentPage";
export default async function Page({params}:{params:Promise<{recordId:string}>}){const {recordId}=await params;if(!/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(recordId))notFound();return <ConsentDocumentPage recordId={recordId.toLowerCase()}/>;}
