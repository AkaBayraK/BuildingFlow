import {SystemStatusWorkspace} from "@/features/system-status/SystemStatusWorkspace";
import {ProvidersPanel} from "@/features/online-payments/ProvidersPanel";
import {AiStatusEntry} from "@/features/ai-status/AiStatusWorkspace";
import {QaCenterEntry} from "@/features/qa-center/QaCenterWorkspace";
export default function Page(){return <><SystemStatusWorkspace/><AiStatusEntry/><QaCenterEntry/><ProvidersPanel/></>;}
