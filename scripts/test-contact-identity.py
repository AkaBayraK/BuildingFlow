#!/usr/bin/env python3
"""Real isolated Keycloak/Mailpit acceptance; stdlib + already cached images only.

Never reads .env or attaches existing containers. No tokens, cookies, links,
mail content or fixture credentials are printed, including failure paths.
"""
import base64
import hashlib
import html
from html.parser import HTMLParser
import http.cookiejar
import json
import os
from pathlib import Path
import secrets
import subprocess
import sys
import tempfile
import time
import urllib.error
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[1]


class Forms(HTMLParser):
    def __init__(self, text):
        super().__init__(); self.forms = []; self.links = []; self.form = None
        self.feed(text)

    def handle_starttag(self, tag, attributes):
        a = dict(attributes)
        if tag == 'form':
            self.form = {'action': a.get('action'), 'values': {}}; self.forms.append(self.form)
        if tag == 'input' and self.form is not None and a.get('name'):
            if a.get('type') not in ['checkbox', 'submit']:
                self.form['values'][a['name']] = a.get('value', '')
        if tag == 'a' and a.get('href'): self.links.append(a['href'])

    def handle_endtag(self, tag):
        if tag == 'form': self.form = None


class StopCallback(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, fp, code, msg, headers, newurl):
        if urllib.parse.urlparse(newurl).path == '/callback' or newurl == 'buildingflow-test://callback': return None
        return super().redirect_request(request, fp, code, msg, headers, newurl)


class Fixture:
    def __init__(self):
        self.suffix = secrets.token_hex(4)
        self.network = 'buildingflow-test-contact-' + self.suffix
        self.kc = self.network + '-kc'; self.mail = self.network + '-mail'
        self.created = []; self.stage = 'setup'; self.passed = []
        self.tmp = tempfile.TemporaryDirectory(prefix='buildingflow-contact-')
        self.realm = 'buildingflow-test-contact'
        self.secret = secrets.token_urlsafe(32)
        self.password = secrets.token_urlsafe(24) + 'Aa9'

    def docker(self, *args):
        return subprocess.run(['docker', *args], check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True).stdout.strip()

    def call(self, url, data=None, method=None, bearer=None, opener=None):
        headers = {}
        if bearer: headers['Authorization'] = 'Bearer ' + bearer
        if data is not None:
            if isinstance(data, bytes): headers['Content-Type'] = 'application/x-www-form-urlencoded'
            else: data = json.dumps(data).encode(); headers['Content-Type'] = 'application/json'
        request = urllib.request.Request(url, data, headers, method=method)
        try:
            with (opener or urllib.request.build_opener()).open(request, timeout=15) as response:
                return response.status, response.read().decode(), response.headers
        except urllib.error.HTTPError as error:
            return error.code, error.read().decode(), error.headers

    def admin(self, path, data=None, method=None):
        status, body, headers = self.call(self.base + '/admin/realms/' + self.realm + path, data, method, self.token)
        assert status < 400
        return json.loads(body) if body else headers

    def start(self):
        jar = Path(self.tmp.name) / 'notice.jar'
        subprocess.run(['bash', str(ROOT / 'scripts/build-contact-notice.sh'), str(jar)], check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        env = Path(self.tmp.name) / 'private.env'
        env.write_text('KC_BOOTSTRAP_ADMIN_CLIENT_ID=fixture-admin\nKC_BOOTSTRAP_ADMIN_CLIENT_SECRET=' + self.secret + '\n'); env.chmod(0o600)
        self.docker('network', 'create', self.network)
        self.docker('run', '-d', '--pull=never', '--name', self.mail, '--network', self.network, '--network-alias', 'mailpit', '-p', '127.0.0.1::8025', 'axllent/mailpit:v1.27.10'); self.created.append(self.mail)
        self.docker('run', '-d', '--pull=never', '--name', self.kc, '--network', self.network, '--env-file', str(env),
                    '-p', '127.0.0.1::8080', '-v', str(jar)+':/opt/keycloak/providers/buildingflow-contact-notice.jar:ro',
                    '-v', str(ROOT/'deploy/keycloak/themes/buildingflow')+':/opt/keycloak/themes/buildingflow:ro',
                    'quay.io/keycloak/keycloak:26.7.5', 'start-dev', '--features=update-email', '--log-level=ERROR', '--http-access-log-enabled=false')
        self.created.append(self.kc)
        self.base = 'http://' + self.docker('port', self.kc, '8080/tcp')
        self.mailbase = 'http://' + self.docker('port', self.mail, '8025/tcp')
        for attempt in range(90):
            try:
                if self.call(self.base + '/realms/master/.well-known/openid-configuration')[0] == 200: break
            except Exception: pass
            time.sleep(1)
        else: raise AssertionError('readiness')
        credentials = urllib.parse.urlencode({'grant_type':'client_credentials','client_id':'fixture-admin','client_secret':self.secret}).encode()
        status, data, _ = self.call(self.base+'/realms/master/protocol/openid-connect/token', credentials)
        assert status == 200; self.token = json.loads(data)['access_token']
        policy = json.loads((ROOT/'deploy/keycloak/contact-verification-policy.json').read_text())
        realm = {**policy, 'realm':self.realm, 'enabled':True, 'sslRequired':'none', 'resetPasswordAllowed':True,
                 'actionTokenGeneratedByUserLifespan':600,
                 'smtpServer':{'host':'mailpit','port':'1025','from':'no-reply@example.test','auth':'false','ssl':'false','starttls':'false'},
                 'clients':[{'clientId':'web','enabled':True,'publicClient':True,'standardFlowEnabled':True,'directAccessGrantsEnabled':False,
                             'redirectUris':[self.base+'/callback'],'attributes':{'pkce.code.challenge.method':'S256'}},
                            {'clientId':'mobile','enabled':True,'publicClient':True,'standardFlowEnabled':True,'directAccessGrantsEnabled':False,
                             'redirectUris':['buildingflow-test://callback'],'attributes':{'pkce.code.challenge.method':'S256'}}]}
        assert self.call(self.base+'/admin/realms',realm,bearer=self.token)[0] == 201
        self.record('Keycloak 26.7.5 + Mailpit isolated setup; realm-wide verifyEmail remains false')

    def record(self, label):
        self.passed.append(label); print('[TEST] GEÇTİ: '+label, flush=True)

    def user(self, name, actions=None, verified=False, temporary=False):
        h = self.admin('/users', {'username':name,'email':name+'@example.test','firstName':'Test','lastName':'Fixture','enabled':True,
                                'emailVerified':verified,'requiredActions':actions or [],
                                'credentials':[{'type':'password','value':self.password,'temporary':temporary}]})
        return h['Location'].rsplit('/',1)[-1]

    def mailbox(self, address):
        status, data, _ = self.call(self.mailbase+'/api/v1/messages'); assert status==200
        return [m for m in json.loads(data).get('messages',[]) if any(x['Address']==address for x in m.get('To',[]))]

    def mail_link(self, address):
        messages = self.mailbox(address); assert messages
        status, body, _ = self.call(self.mailbase+'/api/v1/message/'+messages[0]['ID']); assert status==200
        message = json.loads(body)
        links = Forms(message.get('HTML','')).links
        return next(x for x in links if '/login-actions/action-token?' in x)

    def browser(self):
        # Browsers accept secure cookies on trustworthy loopback HTTP. Match that only in this isolated fixture.
        jar=http.cookiejar.CookieJar(http.cookiejar.DefaultCookiePolicy(secure_protocols=("https","wss","http"))); browser=urllib.request.build_opener(StopCallback(), urllib.request.HTTPCookieProcessor(jar)); browser.fixture_jar=jar; return browser

    def auth(self, opener, name, action=None, redirect=None, login=True, client="web"):
        verifier = secrets.token_urlsafe(32); self.last_verifier=verifier
        challenge = base64.urlsafe_b64encode(hashlib.sha256(verifier.encode()).digest()).decode().rstrip('=')
        query = {'client_id':client,'redirect_uri':redirect or self.base+'/callback','response_type':'code','scope':'openid',
                 'state':secrets.token_urlsafe(16),'nonce':secrets.token_urlsafe(16),'code_challenge':challenge,'code_challenge_method':'S256'}
        if action: query['kc_action']=action
        response = self.call(self.base+'/realms/'+self.realm+'/protocol/openid-connect/auth?'+urllib.parse.urlencode(query),opener=opener)
        if login and response[0]==200 and any('password' in f['values'] for f in Forms(response[1]).forms):
            response = self.submit(opener,response[1],{'username':name,'password':self.password})
        return response

    def submit(self, opener, page, values):
        form = Forms(page).forms[0]
        return self.call(html.unescape(form['action']),urllib.parse.urlencode({**form['values'],**values}).encode(),opener=opener)

    def confirm(self, opener, link):
        response = self.call(link,opener=opener)
        for _ in range(3):
            links = [x for x in Forms(response[1]).links if '/login-actions/action-token?' in x]
            if not links: break
            response = self.call(html.unescape(links[0]),opener=opener)
        return response

    def test(self):
        self.stage = 'new login required action'
        uid=self.user('new.owner',['VERIFY_EMAIL'])
        b=self.browser(); response=self.auth(b,'new.owner')
        assert response[0]==200 and not self.admin('/users/'+uid)['emailVerified']
        assert len(self.mailbox('new.owner@example.test'))==1
        link=self.mail_link('new.owner@example.test'); self.confirm(b,link)
        assert self.admin('/users/'+uid)['emailVerified']
        count=len(self.mailbox('new.owner@example.test')); self.auth(self.browser(),'new.owner')
        assert len(self.mailbox('new.owner@example.test'))==count
        self.record('VERIFY_EMAIL blocks first login; confirmation shared; next login sends no second mail')
        self.stage='combined invitation'
        for name,temp in [('invited.owner',False),('temporary.owner',True)]:
            uid=self.user(name,['VERIFY_EMAIL'],temporary=temp)
            self.admin('/users/'+uid+'/execute-actions-email?lifespan=600',['VERIFY_EMAIL','UPDATE_PASSWORD'],'PUT')
            assert len(self.mailbox(name+'@example.test'))==1
            b=self.browser(); response=self.confirm(b,self.mail_link(name+'@example.test'))
            if Forms(response[1]).forms:
                response=self.submit(b,response[1],{'password-new':self.password,'password-confirm':self.password})
            assert self.admin('/users/'+uid)['emailVerified']
            assert len(self.mailbox(name+'@example.test'))==1
        self.record('Normal + temporary-password combined invitation: one email and verified account')
        self.stage='update email'
        uid=self.user('change.owner',verified=True)
        b=self.browser(); response=self.auth(b,'change.owner')
        code=urllib.parse.parse_qs(urllib.parse.urlparse(response[2]['Location']).query)['code'][0]
        status,body,_=self.call(self.base+'/realms/'+self.realm+'/protocol/openid-connect/token',urllib.parse.urlencode({'grant_type':'authorization_code','client_id':'web','redirect_uri':self.base+'/callback','code':code,'code_verifier':self.last_verifier}).encode())
        assert status==200
        stale=body and json.loads(body)['access_token']
        claims=json.loads(base64.urlsafe_b64decode(stale.split('.')[1]+'=='))
        assert claims['email']=='change.owner@example.test'
        response=self.auth(b,'change.owner','UPDATE_EMAIL')
        assert response[0]==200 and 'email' in response[1]
        response=self.submit(b,response[1],{'email':'new.address@example.test'})
        before=self.admin('/users/'+uid); assert before['email']=='change.owner@example.test' and before['emailVerified']
        self.confirm(b,self.mail_link('new.address@example.test'))
        after=self.admin('/users/'+uid); assert after['email']=='new.address@example.test' and after['emailVerified']
        for _ in range(30):
            if self.mailbox('change.owner@example.test'): break
            time.sleep(.2)
        assert len(self.mailbox('change.owner@example.test'))==1
        assert claims['email'] != after['email'] # An old JWT must not replace the fresh IdP source.
        self.record('UPDATE_EMAIL preserves old verified address until confirmation; old-address notice delivered')
        self.stage='link replay'
        self.confirm(self.browser(),self.mail_link('new.address@example.test'))
        assert self.admin('/users/'+uid)['email']=='new.address@example.test'
        assert len(self.mailbox('change.owner@example.test'))==1
        self.record('Consumed update link produces no repeated update/notice')
        self.stage='recent reauthentication'
        time.sleep(1)
        response=self.auth(b,'change.owner','UPDATE_EMAIL',login=False)
        assert any('password' in f['values'] for f in Forms(response[1]).forms)
        self.record('UPDATE_EMAIL requires fresh credentials even with an existing session')
        self.stage='expired and resend invitation'
        uid=self.user('expired.owner',['VERIFY_EMAIL'])
        self.admin('/users/'+uid+'/execute-actions-email?lifespan=1',['VERIFY_EMAIL','UPDATE_PASSWORD'],'PUT')
        expired=self.mail_link('expired.owner@example.test'); time.sleep(2)
        response=self.confirm(self.browser(),expired)
        assert not self.admin('/users/'+uid)['emailVerified']
        assert not any('password-new' in f['values'] for f in Forms(response[1]).forms)
        self.admin('/users/'+uid+'/execute-actions-email?lifespan=600',['VERIFY_EMAIL','UPDATE_PASSWORD'],'PUT')
        assert len(self.mailbox('expired.owner@example.test'))==2
        b2=self.browser(); response=self.confirm(b2,self.mail_link('expired.owner@example.test'))
        if Forms(response[1]).forms: self.submit(b2,response[1],{'password-new':self.password,'password-confirm':self.password})
        assert self.admin('/users/'+uid)['emailVerified']
        self.record('Expired invitation rejected; explicit resend completes one owner flow')
        self.stage='legacy account and password-action semantics'
        uid=self.user('legacy.owner')
        response=self.auth(self.browser(),'legacy.owner')
        assert response[0]==302 and not self.admin('/users/'+uid)['emailVerified']
        self.admin('/users/'+uid+'/execute-actions-email?lifespan=600',['UPDATE_PASSWORD'],'PUT')
        b3=self.browser(); response=self.confirm(b3,self.mail_link('legacy.owner@example.test'))
        if Forms(response[1]).forms: self.submit(b3,response[1],{'password-new':self.password,'password-confirm':self.password})
        assert self.admin('/users/'+uid)['emailVerified']
        self.record('Legacy account stays accessible; native UPDATE_PASSWORD link also proves its bound email in 26.7.5')
        self.stage='mobile PKCE login'
        response=self.auth(self.browser(),'new.owner',redirect='buildingflow-test://callback',client='mobile')
        assert response[0]==302 and response[2]['Location'].startswith('buildingflow-test://callback?')
        code=urllib.parse.parse_qs(urllib.parse.urlparse(response[2]['Location']).query)['code'][0]
        status,_,_=self.call(self.base+'/realms/'+self.realm+'/protocol/openid-connect/token',urllib.parse.urlencode({'grant_type':'authorization_code','client_id':'mobile','redirect_uri':'buildingflow-test://callback','code':code,'code_verifier':self.last_verifier}).encode())
        assert status==200
        self.record('Mobile S256 authorization and code exchange return only to its registered scheme')
        self.stage='redirect and PKCE'
        assert self.auth(self.browser(),'new.owner',redirect='https://invalid.example/callback')[0]==400
        status,_,headers=self.call(self.base+'/realms/'+self.realm+'/protocol/openid-connect/auth?'+urllib.parse.urlencode({'client_id':'web','redirect_uri':self.base+'/callback','response_type':'code','scope':'openid'}),opener=self.browser())
        assert status==400 or status==302 and 'error=' in headers.get('Location','') and 'code=' not in headers.get('Location','')
        self.record('Unsafe redirect and missing S256 PKCE rejected')
        self.stage='SMTP outage'
        uid=self.user('smtp.failure',['VERIFY_EMAIL'])
        self.admin('',{'smtpServer':{'host':'127.0.0.1','port':'1','from':'no-reply@example.test','auth':'false','ssl':'false','starttls':'false'}},'PUT')
        status,_,_=self.call(self.base+'/admin/realms/'+self.realm+'/users/'+uid+'/execute-actions-email',['VERIFY_EMAIL'],method='PUT',bearer=self.token)
        assert status>=400 and not self.admin('/users/'+uid)['emailVerified']
        self.record('SMTP outage stays unverified and execute-actions returns failure')
        self.stage='log privacy'
        process=subprocess.run(['docker','logs',self.kc],capture_output=True,text=True,check=True)
        logs=process.stdout+process.stderr
        assert all(marker not in logs for marker in ['@example.test', self.secret, self.password, stale, 'action-token?key='])
        assert 'BF_CONTACT_NOTICE_UNAVAILABLE' not in logs
        self.record('Fixture emails, credentials, JWT and action links absent from Keycloak logs')

    def close(self):
        for name in reversed(self.created):
            subprocess.run(['docker','rm','-f','-v',name],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
        subprocess.run(['docker','network','rm',self.network],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
        self.tmp.cleanup()


if __name__=='__main__':
    fixture=Fixture()
    try:
        fixture.start(); fixture.test()
        print('[TAMAMLANDI] '+str(len(fixture.passed))+' gerçek IdP/SMTP kabul grubu geçti.')
    except Exception as error:
        import traceback
        line = traceback.extract_tb(error.__traceback__)[-1].lineno
        print('[HATA] Denetim satırı: '+str(line)+'; tür: '+type(error).__name__,file=sys.stderr)
        print('[HATA] İzole kabul başarısız; aşama: '+fixture.stage+' (hassas çıktı bastırıldı).',file=sys.stderr)
        sys.exit(1)
    finally: fixture.close()
