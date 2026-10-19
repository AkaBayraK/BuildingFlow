#!/usr/bin/env python3
"""Validate a production env file before deployment. Prints rule names only, never values.

Usage: python3 scripts/release-preflight.py <production.env>
"""
import ipaddress,re,sys
from urllib.parse import urlsplit
LOCAL={'localhost','127.0.0.1','::1','mailpit'}
def parse(path):
 values={}
 for n,line in enumerate(open(path,encoding='utf-8'),1):
  line=line.strip()
  if not line or line.startswith('#'):continue
  if '=' not in line:raise SystemExit(f'FAIL: line {n} is not KEY=VALUE')
  k,v=line.split('=',1);values[k.strip()]=v.strip().strip('"').strip("'")
 return values
def https(value,origin=False):
 try:u=urlsplit(value or '')
 except ValueError:return False
 return u.scheme=='https' and bool(u.hostname) and u.hostname not in LOCAL and not u.username and not u.password and not u.query and not u.fragment and (not origin or u.path in ('','/'))
def indexed(env,prefix):return [v for k,v in sorted(env.items()) if re.fullmatch(re.escape(prefix)+r'__\d+',k)]
def single_ip(value):
 try:ip=ipaddress.ip_address(value)
 except ValueError:return False
 return not ip.is_unspecified
def secret(value):return len(value or '')>=16
def connection_password(value):
 m=re.search(r'(?:^|;)\s*Password=([^;]*)',value or '',re.I);return bool(m) and secret(m.group(1))
def rules(env):
 smtp=env.get('Smtp__Enabled','').lower()=='true'
 hosts=[h.strip() for h in env.get('AllowedHosts','').split(';') if h.strip()]
 yield 'Production environment',env.get('ASPNETCORE_ENVIRONMENT')=='Production'
 yield 'HTTPS OIDC authority',https(env.get('Authentication__Authority'))
 yield 'HTTPS or empty OIDC metadata',not env.get('Authentication__MetadataAddress') or https(env.get('Authentication__MetadataAddress'))
 yield 'API audience set',bool(env.get('Authentication__Audience'))
 yield 'Explicit HTTPS CORS origins',bool(indexed(env,'Cors__Origins')) and all(https(o,True) for o in indexed(env,'Cors__Origins'))
 yield 'Explicit AllowedHosts',bool(hosts) and all('*' not in h and '/' not in h and h not in LOCAL for h in hosts)
 yield 'Forwarded headers only from listed proxy IPs',env.get('ASPNETCORE_FORWARDEDHEADERS_ENABLED','').lower()!='true' and all(single_ip(p) for p in indexed(env,'Security__KnownProxies'))
 yield 'API rate limiting enabled',env.get('Security__RateLimit__Enabled','true').lower()!='false'
 yield 'Web HTTPS mode',env.get('BUILDINGFLOW_PUBLIC_HTTPS')=='true'
 yield 'Web build uses HTTPS API and OIDC',https(env.get('NEXT_PUBLIC_API_URL'),True) and https(env.get('NEXT_PUBLIC_OIDC_AUTHORITY'))
 yield 'Web and API share one issuer',env.get('NEXT_PUBLIC_OIDC_AUTHORITY','').rstrip('/')==env.get('Authentication__Authority','').rstrip('/')
 yield 'Database connection has a strong password',connection_password(env.get('ConnectionStrings__Database'))
 yield 'HTTPS object storage endpoint',https(env.get('ObjectStorage__Endpoint'))
 yield 'Object storage access key set',bool(env.get('ObjectStorage__AccessKey'))
 yield 'Broker and object storage secrets set',secret(env.get('RabbitMQ__Password')) and secret(env.get('ObjectStorage__SecretKey'))
 yield 'Person code HMAC key set',re.fullmatch(r'(?:[0-9a-fA-F]{2}){32,}',env.get('PersonCode__HmacKey','')) is not None
 yield 'Keycloak admin client over HTTPS with secret',not env.get('KeycloakAdmin__BaseUrl') or (env['KeycloakAdmin__BaseUrl'].startswith('https://') and secret(env.get('KeycloakAdmin__ClientSecret')))
 yield 'SMTP uses an external host with STARTTLS',not smtp or (env.get('Smtp__Host','').lower() not in LOCAL|{''} and env.get('Smtp__UseStartTls','true').lower()!='false' and env.get('Smtp__AllowInsecureLocal','false').lower()!='true')
 yield 'SMTP sender and credentials',not smtp or (re.fullmatch(r'[^@\s]+@[^@\s]+\.[^@\s.]+',env.get('Smtp__From','')) is not None and not env.get('Smtp__From','').endswith('.local') and (not env.get('Smtp__Username') or secret(env.get('Smtp__Password'))))
def main(path):
 results=list(rules(parse(path)))
 for name,passed in results:print(('PASS ' if passed else 'FAIL ')+name)
 failed=sum(not p for _,p in results)
 print(f'[TEST] Release preflight {len(results)-failed}/{len(results)}'+(' — PASS' if not failed else ' — FAIL'))
 return 1 if failed else 0
if __name__=='__main__':
 if len(sys.argv)!=2:raise SystemExit(__doc__)
 raise SystemExit(main(sys.argv[1]))
