import {notFound} from "next/navigation";
import {PaymentsWorkspace} from "@/features/payments/PaymentsWorkspace";
import {readUnitQuery} from "@/features/unit-ledger/model";
export default async function Page({params,searchParams}:{params:Promise<{id:string}>;searchParams:Promise<{unitId?:string|string[]}>}){const {id}=await params;const unitId=readUnitQuery((await searchParams).unitId);if(!/^[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}$/i.test(id))notFound();return <PaymentsWorkspace key={id+":"+(unitId??"")} propertyId={id} initialUnitId={unitId}/>;}
