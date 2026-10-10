import {CommunicationWorkspace} from "@/features/communication/CommunicationWorkspace";
export default async function Page({params}:{params:Promise<{id:string}>}){const {id}=await params;return <CommunicationWorkspace propertyId={id} section="documents"/>;}
