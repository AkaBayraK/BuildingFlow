import {createRoot} from 'react-dom/client';
import {Parking} from '../../src/parking/Parking';
import {Controls} from './parking-mocks';
createRoot(document.getElementById('root')!).render(<><Controls/><Parking/></>);
