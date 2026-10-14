"""Small SigV4 utility for migration/backup fixtures. Never logs credentials, keys or object bodies."""
import datetime,hashlib,hmac,urllib.parse,urllib.request,urllib.error,xml.etree.ElementTree as ET
class S3Error(RuntimeError):
 def __init__(self,status,code):self.status=status;self.code=code;super().__init__('S3 operation failed ('+str(status)+')')
class S3:
 def __init__(self,endpoint,key,secret,transport=None):
  self.endpoint=endpoint.rstrip('/');self.key=key;self.secret=secret;self.transport=transport
  u=urllib.parse.urlsplit(self.endpoint)
  if u.scheme not in ('http','https') or not u.netloc or u.path or u.username or u.query or u.fragment:raise ValueError('Invalid S3 endpoint')
  self.host=u.netloc
 def request(self,method,path='/',query=None,body=b'',headers=None):
  now=datetime.datetime.now(datetime.timezone.utc);stamp=now.strftime('%Y%m%dT%H%M%SZ');date=stamp[:8]
  q='&'.join(urllib.parse.quote(str(k),safe='-_.~')+'='+urllib.parse.quote(str(v),safe='-_.~') for k,v in sorted((query or {}).items()))
  uri=urllib.parse.quote(path,safe='/-_.~');payload=hashlib.sha256(body).hexdigest()
  hs={'host':self.host,'x-amz-content-sha256':payload,'x-amz-date':stamp}
  names=';'.join(sorted(hs));canonical=method+'\n'+uri+'\n'+q+'\n'+''.join(k+':'+hs[k]+'\n' for k in sorted(hs))+'\n'+names+'\n'+payload
  scope=date+'/us-east-1/s3/aws4_request';to_sign='AWS4-HMAC-SHA256\n'+stamp+'\n'+scope+'\n'+hashlib.sha256(canonical.encode()).hexdigest()
  def sign(key,msg):return hmac.new(key,msg.encode(),hashlib.sha256).digest()
  signing=sign(sign(sign(sign(('AWS4'+self.secret).encode(),date),'us-east-1'),'s3'),'aws4_request')
  hs['Authorization']='AWS4-HMAC-SHA256 Credential='+self.key+'/'+scope+', SignedHeaders='+names+', Signature='+hmac.new(signing,to_sign.encode(),hashlib.sha256).hexdigest()
  hs.update(headers or {})
  url=self.endpoint+uri+('?' + q if q else '')
  if self.transport:status,data,response_headers=self.transport(method,url,hs,body)
  else:
   try:
    with urllib.request.urlopen(urllib.request.Request(url,data=body if method in ('PUT','POST') else None,headers=hs,method=method),timeout=30) as r:
     data=r.read(64*1024*1024+1);status=r.status;response_headers=dict(r.headers)
   except urllib.error.HTTPError as e:status=e.code;data=e.read(1024*1024);response_headers=dict(e.headers)
  if len(data)>64*1024*1024:raise RuntimeError('S3 object exceeds migration memory limit')
  if status>=400:
   try:code=ET.fromstring(data).findtext('Code','')
   except ET.ParseError:code=''
   raise S3Error(status,code)
  return data,response_headers
 def buckets(self):return [n.text for n in ET.fromstring(self.request('GET')[0]).findall('.//{*}Bucket/{*}Name')]
 def create(self,bucket):self.request('PUT','/'+bucket)
 def objects(self,bucket):
  next=None
  while True:
   query={'list-type':'2'}
   if next:query['continuation-token']=next
   doc=ET.fromstring(self.request('GET','/'+bucket,query)[0])
   for obj in doc.findall('{*}Contents'):yield obj.findtext('{*}Key'),int(obj.findtext('{*}Size','0'))
   if doc.findtext('{*}IsTruncated')!='true':return
   next=doc.findtext('{*}NextContinuationToken')
   if not next:raise RuntimeError('Incomplete S3 object listing')
 def get(self,bucket,key):return self.request('GET','/'+bucket+'/'+key)
 def put(self,bucket,key,data,content_type):self.request('PUT','/'+bucket+'/'+key,body=data,headers={'Content-Type':content_type})
