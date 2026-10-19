import type {MetadataRoute} from 'next';
import {headers} from 'next/headers';
export default async function robots():Promise<MetadataRoute.Robots>{const h=await headers();const host=h.get('x-bf-domain');return {rules:{userAgent:'*',allow:'/',disallow:['/preview','/inquiries']},...(host?{sitemap:'https://'+host+'/sitemap.xml'}:{})};}
