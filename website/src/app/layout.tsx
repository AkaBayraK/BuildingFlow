import type {Metadata} from 'next';
import './globals.css';
export const metadata:Metadata={title:{default:'BuildingFlow · Web siteleri',template:'%s'},description:'Yönetim alanlarının yayınlanmış web siteleri.',robots:{index:false,follow:false}};
export default function Layout({children}:{children:React.ReactNode}){return <html lang="tr"><body><a className="skip" href="#content">İçeriğe atla</a>{children}</body></html>}
