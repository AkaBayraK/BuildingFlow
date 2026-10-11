export const heatingNames={NONE:'heatingNone',STOVE:'heatingStove',GAS_STOVE:'heatingGasStove',FLOOR_HEATER:'heatingFloorHeater',CENTRAL:'heatingCentral',CENTRAL_METERED:'heatingCentralMetered',COMBI:'heatingCombi',UNDERFLOOR:'heatingUnderfloor',AIR_CONDITIONING:'heatingAirConditioning',OTHER:'heatingOther'} as const;
export const parkingNames={OPEN:'parkingOpen',CLOSED:'parkingClosed',OPEN_AND_CLOSED:'parkingBoth',NONE:'parkingNone'} as const;
export const roomNames={STUDIO:'roomStudio',R1_1:'room1_1',R1_5_1:'room1_5_1',R2_0:'room2_0',R2_1:'room2_1',R2_5_1:'room2_5_1',R3_1:'room3_1',R3_5_1:'room3_5_1',R4_1:'room4_1',R4_5_1:'room4_5_1',R5_1:'room5_1',R5_2:'room5_2',R6_PLUS:'room6Plus'} as const;
export const floorNames={BELOW_ENTRANCE:'floorBelowEntrance',BASEMENT:'floorBasement',GROUND:'floorGround',GARDEN:'floorGarden',ENTRANCE:'floorEntrance',HIGH_ENTRANCE:'floorHighEntrance',REGULAR:'floorRegular',ROOF:'floorRoof'} as const;
export const kitchenNames={OPEN:'kitchenOpen',CLOSED:'kitchenClosed'} as const;
export const booleanNames={true:'featureYes',false:'featureNo'} as const;
export type BuildingDetails={constructionYear?:number|null;heatingType?:keyof typeof heatingNames|null;hasElevator?:boolean|null;parkingType?:keyof typeof parkingNames|null};
export type UnitDetails={netSquareMeters?:number|null;roomLayout?:keyof typeof roomNames|null;floorPosition?:keyof typeof floorNames|null;isMezzanine?:boolean|null;bathroomCount?:number|null;kitchenType?:keyof typeof kitchenNames|null;hasBalcony?:boolean|null;heatingType?:keyof typeof heatingNames|null};
export type DetailFieldSpec={key:string;label:string;type:'text'|'integer'|'decimal'|'choice'|'boolean';options?:Record<string,string>;maxLength?:number;unit?:string;errorCode?:string};
export const buildingDetailFields:DetailFieldSpec[]=[{key:'constructionYear',label:'constructionYear',type:'integer',errorCode:'BF_CONSTRUCTION_YEAR_INVALID'},{key:'heatingType',label:'heatingType',type:'choice',options:heatingNames,errorCode:'BF_HEATING_TYPE_INVALID'},{key:'hasElevator',label:'hasElevator',type:'boolean',options:booleanNames},{key:'parkingType',label:'parkingType',type:'choice',options:parkingNames,errorCode:'BF_PARKING_TYPE_INVALID'}];
export const unitDetailFields:DetailFieldSpec[]=[{key:'netSquareMeters',label:'netArea',type:'decimal',unit:'m²',errorCode:'BF_NET_AREA_INVALID'},{key:'roomLayout',label:'roomLayout',type:'choice',options:roomNames,errorCode:'BF_ROOM_LAYOUT_INVALID'},{key:'floorPosition',label:'floorPosition',type:'choice',options:floorNames,errorCode:'BF_FLOOR_POSITION_INVALID'},{key:'isMezzanine',label:'isMezzanine',type:'boolean',options:booleanNames},{key:'bathroomCount',label:'bathroomCount',type:'integer',options:{'0':'bathroom0','1':'bathroom1','2':'bathroom2','3':'bathroom3','4':'bathroom4','5':'bathroom5','6':'bathroom6'},errorCode:'BF_BATHROOM_COUNT_INVALID'},{key:'kitchenType',label:'kitchenType',type:'choice',options:kitchenNames,errorCode:'BF_KITCHEN_TYPE_INVALID'},{key:'hasBalcony',label:'hasBalcony',type:'boolean',options:booleanNames},{key:'heatingType',label:'heatingType',type:'choice',options:heatingNames,errorCode:'BF_HEATING_TYPE_INVALID'}];
export const neighborhoodField:DetailFieldSpec={key:'neighborhood',label:'neighborhood',type:'text',maxLength:100};
export function detailFields(kind:string,propertyType='APARTMENT'):DetailFieldSpec[]{return kind==='unit'?unitDetailFields:kind==='building'?buildingDetailFields:kind==='property'?[neighborhoodField,...(propertyType==='APARTMENT'?buildingDetailFields:[])]:[];}
export function detailValues(source:unknown,kind:string):Record<string,string>{const r=(source??{}) as Record<string,unknown>;return Object.fromEntries(detailFields(kind).map(f=>[f.key,r[f.key]==null?'':String(r[f.key])]));}
export function floorMatches(position:string,floor:number){return ['BELOW_ENTRANCE','BASEMENT'].includes(position)?floor<0:['GROUND','GARDEN','ENTRANCE','HIGH_ENTRANCE'].includes(position)?floor===0:floor>=1;}
export function detailPayload(values:Record<string,string>,kind:string,type='APARTMENT'):Record<string,unknown>{const body:Record<string,unknown>={};for(const f of detailFields(kind)){const v=values[f.key]?.trim()??'';body[f.key]=(kind==='property'&&type!=='APARTMENT'&&f.key!=='neighborhood')||!v?null:f.type==='boolean'?v==='true':f.type==='integer'||f.type==='decimal'?Number(v.replace(',','.')):v;}return body;}
export function validateDetailValues(values:Record<string,string>,kind:string,type='APARTMENT',floor=0,gross=0,year=Number(new Intl.DateTimeFormat('en',{year:'numeric',timeZone:'Europe/Istanbul'}).format(new Date()))):Record<string,string>{
 const errors:Record<string,string>={};for(const f of detailFields(kind,type)){const v=values[f.key]?.trim()??'';if(!v)continue;
 if(f.type==='text'&&v.length>(f.maxLength??Infinity))errors[f.key]='detailTextTooLong';
 else if(f.options&&!Object.hasOwn(f.options,v))errors[f.key]=f.errorCode??'BF_INVALID_REQUEST';
 else if(f.type==='integer'&&(!/^\d+$/.test(v)||!Number.isSafeInteger(Number(v))))errors[f.key]=f.errorCode??'BF_INVALID_REQUEST';
 else if(f.type==='decimal'&&!/^\d{1,8}([.,]\d{1,2})?$/.test(v))errors[f.key]=f.errorCode??'BF_INVALID_REQUEST';
 }
 const b=detailPayload(values,kind,type);if(b.constructionYear!=null&&(Number(b.constructionYear)<1800||Number(b.constructionYear)>year))errors.constructionYear='BF_CONSTRUCTION_YEAR_INVALID';
 if(b.bathroomCount!=null&&(Number(b.bathroomCount)<0||Number(b.bathroomCount)>6))errors.bathroomCount='BF_BATHROOM_COUNT_INVALID';
 if(b.netSquareMeters!=null&&(!Number.isFinite(Number(b.netSquareMeters))||Number(b.netSquareMeters)<=0||Number(b.netSquareMeters)>gross))errors.netSquareMeters='BF_NET_AREA_INVALID';
 if(b.floorPosition!=null&&!floorMatches(String(b.floorPosition),floor))errors.floorPosition='BF_FLOOR_POSITION_INVALID';return errors;
}
export function validateDetailResponse(row:Record<string,unknown>,kind:string,propertyType='APARTMENT'){
 for(const f of detailFields(kind)){const v=row[f.key];if(v==null)continue;
 if(kind==='property'&&propertyType!=='APARTMENT'&&f.key!=='neighborhood')throw new Error('Building details require an apartment');
 if(f.type==='text'){if(typeof v!=='string'||v.length>(f.maxLength??Infinity))throw new Error('Invalid detail text');}
 else if(f.type==='boolean'){if(typeof v!=='boolean')throw new Error('Invalid nullable boolean');}
 else if(f.type==='choice'){if(typeof v!=='string'||!Object.hasOwn(f.options??{},v))throw new Error('Invalid detail choice');}
 else if(typeof v!=='number'||!Number.isFinite(v)||(f.type==='integer'&&!Number.isSafeInteger(v)))throw new Error('Invalid detail number');
 }
 if(row.constructionYear!=null&&(Number(row.constructionYear)<1800||Number(row.constructionYear)>9999))throw new Error('Invalid construction year');
 if(row.netSquareMeters!=null&&(Number(row.netSquareMeters)<=0||Number(row.netSquareMeters)>Number(row.grossSquareMeters)||Math.abs(Number(row.netSquareMeters)*100-Math.round(Number(row.netSquareMeters)*100))>0.000001))throw new Error('Invalid net area');
 if(row.bathroomCount!=null&&(Number(row.bathroomCount)<0||Number(row.bathroomCount)>6))throw new Error('Invalid bathroom count');
 if(row.floorPosition!=null&&!floorMatches(String(row.floorPosition),Number(row.floor)))throw new Error('Invalid floor position');
}
