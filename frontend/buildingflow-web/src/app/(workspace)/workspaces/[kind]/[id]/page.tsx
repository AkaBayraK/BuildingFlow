import {redirect,notFound} from "next/navigation";
import {SessionWorkspace} from "@/features/session-workspaces/SessionWorkspaces";
import {validKind} from "@/features/session-workspaces/model";
import {uuid} from "@/features/person-center/model";
export default async function Page({params}:{params:Promise<{kind:string;id:string}>}){if(process.env.BUILDINGFLOW_SESSION_CONTEXTS_ENABLED!=="true")redirect("/");const p=await params;if(!validKind(p.kind)||!uuid(p.id))notFound();return <SessionWorkspace kind={p.kind} id={p.id.toLowerCase()}/>;}
