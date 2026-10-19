import {redirect} from "next/navigation";
import {SessionWorkspaces} from "@/features/session-workspaces/SessionWorkspaces";
export default function Page(){if(process.env.BUILDINGFLOW_SESSION_CONTEXTS_ENABLED!=="true")redirect("/");return <SessionWorkspaces/>;}
