import {PropertyModuleSelection} from "@/features/navigation/PropertyModuleSelection";
import {CenterList} from "@/features/person-center/CenterList";
export default function PeoplePage(){return (process.env.BUILDINGFLOW_PERSON_CENTER_ENABLED==="true"||process.env.BUILDINGFLOW_SESSION_CONTEXTS_ENABLED==="true")?<CenterList/>:<PropertyModuleSelection section="people"/>;}
