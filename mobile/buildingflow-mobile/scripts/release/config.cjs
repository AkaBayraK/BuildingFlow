const {isIP}=require('node:net');
const REQUIRED=['BF_RELEASE_IOS_BUNDLE_ID','BF_RELEASE_ANDROID_PACKAGE','BF_RELEASE_VERSION','BF_RELEASE_IOS_BUILD_NUMBER','BF_RELEASE_ANDROID_VERSION_CODE','BF_RELEASE_EAS_PROJECT_ID','EXPO_PUBLIC_API_URL','EXPO_PUBLIC_AUTH_ISSUER','EXPO_PUBLIC_AUTH_CLIENT_ID','EXPO_PUBLIC_AUTH_REDIRECT_SCHEME'];
function configurationIssues(env){
 const issues=[];for(const k of REQUIRED)if(typeof env[k]!=='string'||!env[k].trim())issues.push(k+':missing');
 const check=(key,rule)=>{if(env[key]&&!rule(env[key]))issues.push(key+':invalid');};
 check('BF_RELEASE_IOS_BUNDLE_ID',v=>/^[a-zA-Z][a-zA-Z0-9-]*(\.[a-zA-Z][a-zA-Z0-9-]*)+$/.test(v)&&!v.toLowerCase().endsWith('.dev'));
 check('BF_RELEASE_ANDROID_PACKAGE',v=>/^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$/.test(v)&&!v.toLowerCase().endsWith('.dev'));
 check('BF_RELEASE_VERSION',v=>/^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)$/.test(v)&&v!=='0.0.0');
 check('BF_RELEASE_IOS_BUILD_NUMBER',v=>/^[1-9]\d{0,3}(?:\.(?:0|[1-9]\d?)){0,2}$/.test(v));
 check('BF_RELEASE_ANDROID_VERSION_CODE',v=>/^[1-9]\d*$/.test(v)&&Number.isSafeInteger(+v)&&+v<=2100000000);
 check('BF_RELEASE_EAS_PROJECT_ID',v=>/^[a-f0-9]{8}(-[a-f0-9]{4}){3}-[a-f0-9]{12}$/i.test(v)&&!/^0{8}-/.test(v));
 function publicHttps(value){try{const u=new URL(value),h=u.hostname;return u.protocol==='https:'&&!u.username&&!u.password&&!u.search&&!u.hash&&!isIP(h.replace(/^\[|\]$/g,''))&&h.includes('.')&&!/(^|\.)(localhost|local|internal|test|example|invalid)$/.test(h)&&!/(^|\.)example\.(com|net|org)$/.test(h);}catch{return false;}}
 check('EXPO_PUBLIC_API_URL',publicHttps);check('EXPO_PUBLIC_API_WEB_URL',publicHttps);check('EXPO_PUBLIC_AUTH_WEB_ISSUER',v=>publicHttps(v)&&new URL(v).pathname.endsWith('/realms/buildingflow'));check('EXPO_PUBLIC_AUTH_ISSUER',v=>publicHttps(v)&&new URL(v).pathname.endsWith('/realms/buildingflow'));
 check('EXPO_PUBLIC_AUTH_CLIENT_ID',v=>/^[A-Za-z0-9._-]{1,100}$/.test(v));
 check('EXPO_PUBLIC_AUTH_REDIRECT_SCHEME',v=>/^[a-z][a-z0-9+.-]{2,100}$/.test(v)&&!['http','https','exp','buildingflow-dev'].includes(v));
 check('BF_RELEASE_APPLE_TEAM_ID',v=>/^[A-Z0-9]{10}$/.test(v));
 for(const key of Object.keys(env))if(key.startsWith('EXPO_PUBLIC_')&&/(SECRET|PASSWORD|TOKEN|PRIVATE_KEY|ACCESS_KEY)/i.test(key)&&env[key])issues.push('PUBLIC_SECRET:forbidden');
 if(env.EXPO_PUBLIC_VERIFY_OFFLINE&&env.EXPO_PUBLIC_VERIFY_OFFLINE!=='0')issues.push('NATIVE_FIXTURE:forbidden');
 return [...new Set(issues)];
}
function buildAppConfig(base,env){
 const variant=env.BUILDINGFLOW_APP_VARIANT||'development';
 if(!['development','production'].includes(variant))throw Error('BUILDINGFLOW_APP_VARIANT:invalid');
 if(env.EAS_BUILD_PROFILE==='production'&&variant!=='production')throw Error('RELEASE_VARIANT:mismatch');
 if(variant==='development'){if(env.EXPO_PUBLIC_AUTH_REDIRECT_SCHEME&&env.EXPO_PUBLIC_AUTH_REDIRECT_SCHEME!==base.scheme)throw Error('DEV_REDIRECT:mismatch');return {...base};}
 const issues=configurationIssues(env);if(issues.length)throw Error('BuildingFlow release configuration: '+issues.join(', '));
 const ios={...base.ios,bundleIdentifier:env.BF_RELEASE_IOS_BUNDLE_ID,buildNumber:env.BF_RELEASE_IOS_BUILD_NUMBER,infoPlist:{...base.ios?.infoPlist,NSAppTransportSecurity:{NSAllowsArbitraryLoads:false,NSAllowsLocalNetworking:false}}};
 delete ios.appleTeamId;if(env.BF_RELEASE_APPLE_TEAM_ID)ios.appleTeamId=env.BF_RELEASE_APPLE_TEAM_ID;
 const plugins=(base.plugins||[]).map(p=>Array.isArray(p)&&p[0]==='expo-build-properties'?[p[0],{...p[1],android:{...p[1]?.android,usesCleartextTraffic:false}}]:p);
 return {...base,version:env.BF_RELEASE_VERSION,scheme:env.EXPO_PUBLIC_AUTH_REDIRECT_SCHEME,ios,android:{...base.android,package:env.BF_RELEASE_ANDROID_PACKAGE,versionCode:Number(env.BF_RELEASE_ANDROID_VERSION_CODE),allowBackup:false,blockedPermissions:[...new Set([...(base.android?.blockedPermissions||[]),'android.permission.SYSTEM_ALERT_WINDOW','android.permission.RECORD_AUDIO'])]},plugins,extra:{...base.extra,eas:{projectId:env.BF_RELEASE_EAS_PROJECT_ID}},updates:{...base.updates,enabled:false}};
}
module.exports={configurationIssues,buildAppConfig,REQUIRED};
