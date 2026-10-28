import {useState} from 'react';
import {createRoot} from 'react-dom/client';
import {NextIntlClientProvider} from 'next-intl';
import {NotificationProvider} from '../../src/components/feedback/Notifications';
import {ThemeProvider} from '../../src/features/theme/ThemeProvider';
import {CommonAreasWorkspace} from '../../src/features/common-areas/CommonAreasWorkspace';
import tr from '../../messages/tr.json';import en from '../../messages/en.json';
import {P,U,fixtureActions} from './common-area-api';
function Fixture(){const[locale,setLocale]=useState<'tr'|'en'>('tr'),[theme,setTheme]=useState('light');const [readonly,setReadonly]=useState(window.location.pathname==='/readonly');return <ThemeProvider><NextIntlClientProvider locale={locale} messages={locale==='tr'?tr:en} timeZone="Europe/Istanbul"><NotificationProvider><div className={theme==='dark'?'dark':''} style={{background:'var(--background)',color:'var(--foreground)',minHeight:'100vh'}}><div className="flex flex-wrap gap-3 border-b p-3"><strong>Ortak Alanlar — İzole UI kabulü; gerçek API/auth değildir.</strong><button onClick={()=>setLocale(l=>l==='tr'?'en':'tr')}>TR / EN</button><button onClick={()=>{document.documentElement.classList.toggle('dark');setTheme(t=>t==='dark'?'light':'dark');}}>Tema</button><button onClick={()=>fixtureActions.conflict()}>Fixture çakışma üret</button><a href="/readonly" onClick={e=>{e.preventDefault();history.pushState({},'','/readonly');setReadonly(true);}}>Salt okunur kabul</a></div><main className="mx-auto max-w-6xl px-4"><CommonAreasWorkspace propertyId={P} scopeName="Örnek Yönetim Alanı" context={readonly?{propertyId:P,unitId:U,capacity:'RESIDENT',relationId:U}:undefined}/></main></div></NotificationProvider></NextIntlClientProvider></ThemeProvider>;}
createRoot(document.getElementById('root')!).render(<Fixture/>);
