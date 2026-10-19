import {websiteMetadata,Structured} from '../../../lib/seo';
import {fetchListings} from '../../../lib/listings';
import type {Metadata} from 'next';
import {Residence} from '../../../components/Residence';
import {notFound} from 'next/navigation';
import {getSite} from '../../../lib/site';
import {validSlug} from '../../../lib/public-site';
type Props={params:Promise<{siteKey:string;slug?:string[]}>};
async function resolve(params:Props['params']){const {siteKey,slug=[]}=await params;if(slug.length>1 || (slug[0] && !validSlug(slug[0]))) notFound();const site=await getSite(siteKey);const page=site.pages.find(p=>p.slug===(slug[0]??''));if(!page) notFound();return {site,page};}
export async function generateMetadata({params}:Props):Promise<Metadata>{const {site,page}=await resolve(params);return websiteMetadata(site,page,page.slug?'/'+page.slug:'/');}
export default async function SitePage({params}:Props){const {site,page}=await resolve(params);const listings=page.sections.some(s=>s.type==='LISTINGS')?await fetchListings(site.siteKey):[];return <><Structured site={site} path={page.slug?'/'+page.slug:'/'}/><Residence site={site} page={page} listings={listings??[]}/></>}
