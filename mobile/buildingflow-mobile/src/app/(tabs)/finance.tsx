import {useAuth} from '../../auth/AuthProvider';
import {FinanceScreen} from '../../components/FinancePanel';
import {ModuleScreen} from '../../components/Screen';
import {financeRoles} from '../../finance/contracts';
export default function Finance(){const a=useAuth();if(a.selected&&financeRoles.includes(a.selected.role))return <FinanceScreen/>;return <ModuleScreen title="Aidat ve ödemeler" subtitle="Finans kayıtları" icon="wallet-outline" description="Finans kayıtlarını görmek için yetkili bir daire veya yönetim bağlantısı gerekir." items={['Yetkili mülk ve rol seçimi','Dönem ve tahsilat geçmişi']}/>;}
