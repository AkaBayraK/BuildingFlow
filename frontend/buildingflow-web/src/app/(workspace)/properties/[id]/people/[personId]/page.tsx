import {PersonPage} from "@/features/people/PersonPage";
export default async function Page({params}:{params:Promise<{id:string;personId:string}>}){const {id,personId}=await params;return <PersonPage propertyId={id} personId={personId}/>;}
