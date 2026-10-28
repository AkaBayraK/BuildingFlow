import {useState} from 'react';
import {createRoot} from 'react-dom/client';
import {NextIntlClientProvider} from 'next-intl';
import {NotificationProvider} from '../../src/components/feedback/Notifications';
import {ThemeProvider} from '../../src/features/theme/ThemeProvider';
import {ParkingWorkspace} from '../../src/features/parking/ParkingWorkspace';
import {fixtureActions} from './parking-api';
import {P} from './parking-mocks';
import tr from '../../messages/tr.json';import en from '../../messages/en.json';
function Fixture(){const[locale,setLocale]=useState<'tr'|'en'>('tr');return <ThemeProvider><NextIntlClientProvider locale={locale} messages={locale==='tr'?tr:en} timeZone="Europe/Istanbul"><NotificationProvider><div className="min-h-screen bg-background text-foreground"><div className="flex flex-wrap gap-3 border-b p-3"><strong>Otopark — İzole UI testi; gerçek API/auth yok</strong><button onClick={()=>setLocale(l=>l==='tr'?'en':'tr')}>TR / EN</button><button onClick={()=>document.documentElement.classList.toggle('dark')}>Tema</button><button onClick={()=>fixtureActions.conflict()}>409 üret</button><a href="/guard">Güvenlik</a></div><main className="mx-auto max-w-6xl px-4"><ParkingWorkspace propertyId={P}/></main></div></NotificationProvider></NextIntlClientProvider></ThemeProvider>;}
createRoot(document.getElementById('root')!).render(<Fixture/>);
