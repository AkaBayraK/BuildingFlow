import { AuthGate } from "../../auth/AuthGate";
import { WorkspaceShell } from "../../components/WorkspaceShell";
export default function WorkspaceLayout({children}:{children:React.ReactNode}) {
  return <AuthGate><WorkspaceShell>{children}</WorkspaceShell></AuthGate>;
}
