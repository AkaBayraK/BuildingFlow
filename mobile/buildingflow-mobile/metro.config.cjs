const {getDefaultConfig}=require('expo/metro-config');
const path=require('node:path');
const config=getDefaultConfig(__dirname);
// This repo has independently installed clients, not root package workspaces.
// Only the dependency-free shared contact contract lives outside the Expo root.
config.watchFolders=[...(config.watchFolders??[]),path.resolve(__dirname,'../../shared')];
module.exports=config;
