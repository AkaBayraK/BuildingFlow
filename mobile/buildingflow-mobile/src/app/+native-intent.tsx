import {safeIncomingPath} from '../security/incoming';
export function redirectSystemPath({path}:{path:string;initial:boolean}){return safeIncomingPath(path);}
