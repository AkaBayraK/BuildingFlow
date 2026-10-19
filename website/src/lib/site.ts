import 'server-only';
import {cache} from 'react';
import {notFound} from 'next/navigation';
import {fetchSite} from './public-site';
export const getSite=cache(async(key:string)=>{const site=await fetchSite(key,process.env.WEBSITE_API_ORIGIN ?? 'http://localhost:5005');if(!site) notFound();return site;});
