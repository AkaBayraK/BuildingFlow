import {useAuth} from '../../auth/AuthProvider';
import {Screen,ModuleScreen} from '../../components/Screen';
import {RequestForm} from '../../components/RequestForm';
import {RequestBoard} from '../../components/RequestBoard';
import {SyncPanel} from '../../components/SyncPanel';
import {ManagerPanel} from '../../components/ManagerPanel';
import {managerRoles} from '../../manager/contracts';
export default function Requests(){const a=useAuth();if(!a.selected||a.selected.role==='ACCOUNTANT')return <ModuleScreen title="Talepler" subtitle="Bakım ve arıza" icon="construct-outline" description="Talep oluşturmak için yetkili mülk bağlantısı gerekir." items={['Yetkili mülk seçimi']}/>;return <Screen title="Talep ve arıza" subtitle={a.selected.propertyName}><RequestForm key={'form:'+a.session?.userId+':'+a.selected.propertyId+':'+a.selected.unitId+':'+a.selected.role}/><SyncPanel/><RequestBoard key={'board:'+a.session?.userId+':'+a.selected.propertyId+':'+a.selected.unitId+':'+a.selected.role}/>{managerRoles.includes(a.selected.role)&&<ManagerPanel key={'management:'+a.selected.propertyId+':'+a.selected.role} initialSection="requests"/>}</Screen>;}
