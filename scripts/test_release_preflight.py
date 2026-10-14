#!/usr/bin/env python3
"""Release preflight must accept a complete HTTPS configuration and reject each unsafe deviation without echoing values."""
import pathlib,secrets,subprocess,sys,tempfile
root=pathlib.Path(__file__).resolve().parent.parent
tool=root/'scripts/release-preflight.py'
def filled():
 lines=[]
 for line in (root/'deploy/production.env.example').read_text().splitlines():
  if line.endswith(('Password=','RabbitMQ__Password=','ObjectStorage__SecretKey=')) or line=='Smtp__Username=':
   line+=('smtp-user' if line=='Smtp__Username=' else secrets.token_hex(16))
  lines.append(line)
 return dict(l.split('=',1) for l in lines if l and not l.startswith('#'))
def run(env):
 with tempfile.TemporaryDirectory() as d:
  f=pathlib.Path(d,'production.env');f.write_text('\n'.join(f'{k}={v}' for k,v in env.items()))
  r=subprocess.run([sys.executable,str(tool),str(f)],capture_output=True,text=True)
  leaked=[v for v in env.values() if len(v)>=16 and v in r.stdout+r.stderr]
  if leaked:raise SystemExit('FAIL: preflight printed a configuration value')
  return r.returncode
base=filled()
if run(base)!=0:raise SystemExit('FAIL: valid production template rejected')
if run({k:v for k,v in filled().items() if not k.startswith('Smtp__')} )!=0:raise SystemExit('FAIL: disabled SMTP should not require SMTP settings')
cases={'development':('ASPNETCORE_ENVIRONMENT','Development'),'http authority':('Authentication__Authority','http://auth.example.com/realms/buildingflow'),
 'http metadata':('Authentication__MetadataAddress','http://auth.example.com/m'),'cors path':('Cors__Origins__0','https://app.example.com/x'),
 'cors wildcard':('Cors__Origins__0','*'),'hosts wildcard':('AllowedHosts','*'),'hosts localhost':('AllowedHosts','localhost'),
 'proxy cidr':('Security__KnownProxies__0','10.0.0.0/8'),'proxy any':('Security__KnownProxies__0','0.0.0.0'),
 'auto forwarded':('ASPNETCORE_FORWARDEDHEADERS_ENABLED','true'),'rate limit off':('Security__RateLimit__Enabled','false'),
 'web http mode':('BUILDINGFLOW_PUBLIC_HTTPS','false'),'web http api':('NEXT_PUBLIC_API_URL','http://api.example.com'),
 'issuer mismatch':('NEXT_PUBLIC_OIDC_AUTHORITY','https://other.example.com/realms/buildingflow'),
 'weak db password':('ConnectionStrings__Database','Host=db;Database=b;Username=u;Password=short'),'empty broker secret':('RabbitMQ__Password',''),
 'http storage':('ObjectStorage__Endpoint','http://objects.example.com'),'empty storage key':('ObjectStorage__AccessKey',''),'empty storage secret':('ObjectStorage__SecretKey',''),
 'smtp local':('Smtp__Host','mailpit'),'smtp plaintext':('Smtp__UseStartTls','false'),'smtp insecure':('Smtp__AllowInsecureLocal','true'),
 'smtp local sender':('Smtp__From','no-reply@buildingflow.local'),'smtp missing password':('Smtp__Password','')}
accepted=[name for name,(k,v) in cases.items() if run(dict(base,**{k:v}))==0]
if accepted:raise SystemExit(f'FAIL: unsafe production settings accepted: {accepted}')
print(f'[TEST] Release preflight: valid template accepted, {len(cases)} unsafe variants rejected, no values printed — PASS')
