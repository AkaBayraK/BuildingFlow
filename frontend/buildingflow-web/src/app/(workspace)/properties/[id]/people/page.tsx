import {PeopleWorkspace} from "@/features/people/PeopleWorkspace";
export default async function Page({params}:{params:Promise<{id:string}>}){const {id}=await params;return <PeopleWorkspace key={id} propertyId={id}/>;}
