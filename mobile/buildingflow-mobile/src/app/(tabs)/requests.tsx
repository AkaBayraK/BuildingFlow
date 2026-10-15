import {ManagerScreen} from '../../components/ManagerPanel';
import {managerRoles} from '../../manager/contracts';
import {useAuth} from '../../auth/AuthProvider';
import {OwnerPanel} from '../../components/OwnerPanel';
import {Screen,ModuleScreen} from '../../components/Screen';
export default function Requests() { const auth=useAuth();if(auth.selected&&managerRoles.includes(auth.selected.role))return <ManagerScreen section="requests"/>;if(auth.selected&&['OWNER','TENANT'].includes(auth.selected.role))return <Screen title="Taleplerim" subtitle={auth.selected.role==='TENANT'?'Kiracı · Seçili daire':'Ev sahibi · Seçili daire'}><OwnerPanel key={auth.selected.propertyId+auth.selected.unitId+auth.selected.role+auth.session?.userId} section="requests"/></Screen>;return <ModuleScreen title="Taleplerim" subtitle="Daha iyi bir bina yaşamı için" icon="construct-outline" tint="#fff0df" description="Bakım ve arıza ihtiyaçlarını yönetime iletip taleplerinin durumunu buradan takip edebileceksin." items={['Bakım ve arıza bildirimi', 'Fotoğraf ve açıklama ekleme', 'Talep durumunu takip etme']} />; }
