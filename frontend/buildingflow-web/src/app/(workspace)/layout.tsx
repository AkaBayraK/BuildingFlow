import { AuthGate } from "../../auth/AuthGate";
import { WorkspaceShell } from "../../components/WorkspaceShell";
export default function WorkspaceLayout({children}:{children:React.ReactNode}) {
  return <AuthGate><WorkspaceShell sessionContexts={process.env.BUILDINGFLOW_SESSION_CONTEXTS_ENABLED==="true"} centralPeople={process.env.BUILDINGFLOW_PERSON_CENTER_ENABLED==="true"||process.env.BUILDINGFLOW_SESSION_CONTEXTS_ENABLED==="true"}>{children}</WorkspaceShell></AuthGate>;
}
