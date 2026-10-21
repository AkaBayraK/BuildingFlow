import {UsersWorkspace} from "../../src/features/users/UsersWorkspace";
import {ThemeProvider} from "../../src/features/theme/ThemeProvider";
import {useState} from "react";
import {AssessmentEditor} from "../../src/features/dues/DuesDialogs";
import {DashboardPage} from '../../src/features/dashboard/DashboardPage';
import {usePathname} from './sidebar-mocks';
import {createRoot} from 'react-dom/client';
import {NextIntlClientProvider} from 'next-intl';
import {WorkspaceShell} from '../../src/components/WorkspaceShell';
import {NotificationProvider} from '../../src/components/feedback/Notifications';
import messages from '../../messages/tr.json';
import {AuthGate} from '../../src/auth/AuthGate';
import {accountChanged} from '../../src/features/account/useAccount';
import {updateFixtureName} from './sidebar-api';
import {navigate} from './sidebar-mocks';

function AssessmentFixture(){const[open,setOpen]=useState(true);return <><button onClick={()=>setOpen(true)}>Yeni tahakkuk aç</button>{open&&<AssessmentEditor root="/api/v1/properties/77777777-7777-4777-8777-777777777777/dues" propertyId="77777777-7777-4777-8777-777777777777" allowParties onClose={()=>setOpen(false)} onSaved={()=>{}}/>}</>;}
function Content(){if(usePathname()==="/users")return <UsersWorkspace/>;if(usePathname()==="/fixture-assessment")return <AssessmentFixture/>;return usePathname()==="/dashboard"?<DashboardPage/>:<section className="py-6"><h1>İzole navigasyon kabulü</h1><p>Gerçek auth/API/veri içermez. Test profili temsilidir.</p><button className="min-h-11 rounded border px-3" onClick={()=>setTimeout(()=>navigate('/reports'),1200)}>Rotayı gecikmeli değiştir</button><button className="min-h-11 rounded border px-3" onClick={()=>{updateFixtureName();accountChanged();}}>Profil fixture güncelle</button><p>Uzun içerik sonu testi</p><div style={{height:1600}}/><p>İçerik sonu</p></section>;}
function Fixture(){return <ThemeProvider><NextIntlClientProvider locale="tr" messages={messages} timeZone="Europe/Istanbul"><NotificationProvider><AuthGate><WorkspaceShell><Content/></WorkspaceShell></AuthGate></NotificationProvider></NextIntlClientProvider></ThemeProvider>;}
createRoot(document.getElementById('root')!).render(<Fixture/>);
