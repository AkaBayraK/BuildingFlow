#!/usr/bin/env python3
"""Real OIDC/native redirect and shared API probe; not a physical device UI test."""
import json,urllib.parse,urllib.request,http.cookiejar,time

def probe(f):
 class NativeRedirect(urllib.request.HTTPRedirectHandler):
  def redirect_request(self,request,fp,code,msg,headers,newurl):
   if newurl.startswith('buildingflow-dev://auth'):return None
   return super().redirect_request(request,fp,code,msg,headers,newurl)
 def browser():
  jar=http.cookiejar.CookieJar(http.cookiejar.DefaultCookiePolicy(secure_protocols=('http','https')))
  return urllib.request.build_opener(NativeRedirect(),urllib.request.HTTPCookieProcessor(jar))
 def exchange(headers):
  params=urllib.parse.parse_qs(urllib.parse.urlparse(headers['Location']).query);assert params.get('code') and headers['Location'].startswith('buildingflow-dev://auth')
  body=urllib.parse.urlencode({'grant_type':'authorization_code','client_id':'mobile','redirect_uri':'buildingflow-dev://auth','code':params['code'][0],'code_verifier':f.last_verifier}).encode()
  status,text,_=f.call(f.base+'/realms/buildingflow/protocol/openid-connect/token',body);assert status==200
  return json.loads(text)['access_token']
 def login(name,b=None,action=None):
  b=b or browser();response=f.auth(b,name,action,redirect='buildingflow-dev://auth',client='mobile');return b,response
 def api(path,token,data=None):
  status,text,_=f.call('http://127.0.0.1:13048/api/v1/mobile/'+path,data,bearer=token);return status,json.loads(text) if text else None
 b,response=login('native.owner');assert response[0]==302;token=exchange(response[2])
 assert api('session',token)[0]==200
 status,contact=api('contact-verification',token);assert status==200 and contact['email']['status']=='UNVERIFIED'
 f.record('Real S256 native-scheme code exchange; API JWT accepted without property membership')
 import uuid
 def command(action,c,code=None):
  body={'operationId':str(uuid.uuid4()),'expectedVersion':c['version'],'action':action}
  if code is not None:body.update(challengeId=c['phone']['challengeId'],code=code)
  status,result=api('contact-verification/phone/commands',token,body);assert status==200;return result
 status,staged=api('contact-verification/phone',token,{'operationId':str(uuid.uuid4()),'expectedVersion':contact['version'],'phone':'05551234567'});assert status==200
 sent=command('SEND',staged['contact']);assert sent['outcome']=='ACCEPTED' and sent['contact']['phone']['status']!='VERIFIED'
 wrong=command('VERIFY',sent['contact'],'000000');assert wrong['outcome']=='REJECTED' and wrong['contact']['phone']['attemptsRemaining']==4
 correct=command('VERIFY',wrong['contact'],'123456');assert correct['outcome']=='VERIFIED'
 f.record('Real mobile API stage/SEND/wrong code/fixed test code confirmation; only test SMS sender')
 status,confirmed=api('contact-verification',token);assert status==200 and confirmed['phone']['status']=='VERIFIED'
 # AIA VerifyEmail starts at the identity screen; explicitly submit resend there.
 response=f.auth(b,'native.owner','VERIFY_EMAIL',redirect='buildingflow-dev://auth',client='mobile')
 assert response[0]==200
 if not f.mailbox('native.owner@example.test'):
  response=f.submit(b,response[1],{})
 link=f.mail_link('native.owner@example.test');confirmation=f.confirm(b,link);assert confirmation[0] in [200,302]
 status,fresh=api('contact-verification',token);assert status==200 and fresh['email']['status']=='VERIFIED' and fresh['phone']['status']=='VERIFIED'
 f.record('Real IdP email confirmation; pre-confirmation JWT reads fresh email and preserves phone proof')
 other,response=login('native.other');assert response[0]==302;other_token=exchange(response[2]);status,other_contact=api('contact-verification',other_token);assert status==200 and other_contact['phone']['status']=='MISSING'
 f.record('Different signed subject reads no prior account phone proof')
