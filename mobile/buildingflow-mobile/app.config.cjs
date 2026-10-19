const {buildAppConfig}=require('./scripts/release/config.cjs');
module.exports=({config})=>buildAppConfig(config,process.env);
