import {ManagerScreen} from '../../components/ManagerPanel';
import {managerRoles} from '../../manager/contracts';
import {useAuth} from '../../auth/AuthProvider';
import {OwnerPanel} from '../../components/OwnerPanel';
import {Screen,ModuleScreen} from '../../components/Screen';
export default function Announcements() { const auth=useAuth();if(auth.selected&&managerRoles.includes(auth.selected.role))return <ManagerScreen section="announcements"/>;if(auth.selected&&['OWNER','TENANT'].includes(auth.selected.role))return <Screen title="Duyurular" subtitle={auth.selected.role==='TENANT'?'Kiracı · Seçili daire':'Ev sahibi · Seçili daire'}><OwnerPanel key={auth.selected.propertyId+auth.selected.unitId+auth.selected.role+auth.session?.userId} section="announcements"/></Screen>;return <ModuleScreen title="Duyurular" subtitle="Binandan haberler, tek bir yerde" icon="megaphone-outline" description="Yönetimin duyuruları ve bina yaşamını ilgilendiren güncel bilgileri burada görebileceksin." items={['Bina duyuruları', 'Yönetimden bilgilendirmeler', 'Duyuru detayları']} />; }
