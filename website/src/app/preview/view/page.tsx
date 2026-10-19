import {notFound} from 'next/navigation';
import {previewSite} from '../../../lib/preview';
import {Residence} from '../../../components/Residence';
export const metadata={title:'Taslak önizlemesi',robots:{index:false,follow:false}};
export default async function Preview({searchParams}:{searchParams:Promise<{slug?:string}>}){const site=await previewSite();if(!site)notFound();const {slug=''}=await searchParams;const page=site.pages.find(p=>p.slug===slug);if(!page)notFound();return <><aside className="demo-bar">TASLAK ÖNİZLEMESİ — Henüz yayınlanmadı. İçerik değişince veya 10 dakika dolunca erişim kapanır.</aside><Residence site={site} page={page} draft/></>;}
