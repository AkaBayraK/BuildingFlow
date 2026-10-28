import React from 'react';
import {createRoot} from 'react-dom/client';
import {CommonAreas} from '../../src/common-areas/CommonAreas';
import {FixtureControls} from './common-area-mocks';
createRoot(document.getElementById('root')!).render(<><header style={{background:'#102342',color:'#fff',padding:24,fontSize:24,fontWeight:700}}>BuildingFlow · Mobil</header><FixtureControls/><main style={{padding:16,maxWidth:540,margin:'auto'}}><CommonAreas/></main></>);
