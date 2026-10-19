#!/usr/bin/env python3
"""Isolated nonroot/read-only website image smoke; removes only its owned container."""
import subprocess,uuid,sys
name='buildingflow-test-website09-'+uuid.uuid4().hex[:8]
created=False
try:
 subprocess.run(['docker','run','-d','--name',name,'--network','none','--read-only','--tmpfs','/tmp:rw,noexec,nosuid,size=32m','--cap-drop','ALL','--security-opt','no-new-privileges','-e','WEBSITE_PUBLIC_ORIGIN=http://localhost:13006','-e','WEBSITE_MANAGEMENT_ORIGIN=http://localhost:3003',sys.argv[1] if len(sys.argv)>1 else 'buildingflow-website:local'],check=True,capture_output=True);created=True
 script=r'''const http=require('http'),assert=require('assert/strict');
const get=(path,host='localhost:13006')=>new Promise((resolve,reject)=>http.get({hostname:'127.0.0.1',port:3000,path,headers:{Host:host}},r=>{let chunks=[];r.on('data',b=>chunks.push(b));r.on('end',()=>resolve({status:r.statusCode,headers:r.headers,bytes:Buffer.concat(chunks)}));}).on('error',reject));
(async()=>{let r;for(let n=0;n<40;n++){try{r=await get('/health');break}catch{await new Promise(r=>setTimeout(r,250));}}assert.equal(r.status,204);assert.ok(r.headers['cache-control'].includes('no-store'));const root=await get('/');assert.equal(root.status,200);assert.ok(root.bytes.length<1024*1024);assert.ok(root.headers['content-security-policy'].includes("object-src 'none'"));const photo=await get('/images/residence.webp');assert.equal(photo.status,200);assert.ok(photo.bytes.length>200000);const bad=await get('/','unknown.example.test');assert.notEqual(bad.status,200);assert.notEqual(process.getuid(),0);const sbom=JSON.parse(require('fs').readFileSync('LICENSES/npm.cdx.json'));assert.equal(sbom.bomFormat,'CycloneDX');const notices=JSON.parse(require('fs').readFileSync('LICENSES/npm-notices/notice-manifest.json'));assert.ok(notices.copied.some(p=>p==='node_modules/next/license.md'||p.toLowerCase()==='node_modules/next/license'));console.log('PASS: container nonroot/read-only/no-network; health204/demo200/photo200/host fail-closed/SBOM/HTML budget');})().catch(e=>{console.error(e.message);process.exitCode=1});'''
 subprocess.run(['docker','exec',name,'node','-e',script],check=True)
finally:
 if created:subprocess.run(['docker','rm','-f',name],check=True,capture_output=True);print('PASS: only owned test container removed')
