import type {Metadata} from 'next';
import {Residence} from '../components/Residence';
import {demoSite} from '../lib/demo';
export const metadata:Metadata={title:'Altunbayrak Rezidans · Şablon önizlemesi',description:'Premium rezidans web sitesi tasarım önizlemesi. Tüm içerik ve görseller temsilidir.',robots:{index:false,follow:false}};
export default function Home(){return <Residence site={demoSite} page={demoSite.pages[0]} preview/>}
