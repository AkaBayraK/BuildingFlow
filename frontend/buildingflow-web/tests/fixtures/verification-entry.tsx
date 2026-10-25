// Isolated browser acceptance: real components/transport, synthetic identity, loopback HTTP API.
import {createRoot} from 'react-dom/client';
import {useState} from 'react';
import {NextIntlClientProvider} from 'next-intl';
import {AuthGate} from '../../src/auth/AuthGate';
import {AccountPage} from '../../src/features/account/AccountPage';
import {NotificationProvider} from '../../src/components/feedback/Notifications';
import {ThemeProvider,useTheme} from '../../src/features/theme/ThemeProvider';
import tr from '../../messages/tr.json';
import en from '../../messages/en.json';
function Fixture(){const[locale,setLocale]=useState<'tr'|'en'>('tr'),[width,setWidth]=useState(1440);const theme=useTheme();return <NextIntlClientProvider locale={locale} messages={locale==='tr'?tr:en} timeZone="Europe/Istanbul"><NotificationProvider><div style={{padding:8}}><label>Fixture language <select value={locale} onChange={e=>setLocale(e.target.value as 'tr'|'en')}><option>tr</option><option>en</option></select></label> <label>Fixture theme <select value={theme.preference} onChange={e=>theme.choose(e.target.value as 'light'|'dark'|'system')}><option>light</option><option>dark</option><option>system</option></select></label> <label>Fixture width <select value={width} onChange={e=>setWidth(Number(e.target.value))}><option>1440</option><option>375</option></select></label> <button onClick={()=>window.dispatchEvent(new Event('focus'))}>Fixture focus return</button> <a href="/fixture-state">Fixture server state</a></div><main style={{width,maxWidth:'100%',margin:'auto'}}><AuthGate><AccountPage/></AuthGate></main></NotificationProvider></NextIntlClientProvider>;}
createRoot(document.getElementById('root')!).render(<ThemeProvider><Fixture/></ThemeProvider>);
