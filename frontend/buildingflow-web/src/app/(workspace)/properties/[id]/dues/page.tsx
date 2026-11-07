import {notFound} from "next/navigation";
import {DuesWorkspace} from "@/features/dues/DuesWorkspace";
import {readUnitQuery} from "@/features/unit-ledger/model";
export default async function Page({params,searchParams}:{params:Promise<{id:string}>;searchParams:Promise<{unitId?:string|string[]}>}){const {id}=await params;const unitId=readUnitQuery((await searchParams).unitId);if(!/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(id))notFound();return <DuesWorkspace key={id+":"+(unitId??"")} propertyId={id} initialUnitId={unitId}/>;}
