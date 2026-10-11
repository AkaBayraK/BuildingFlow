#!/usr/bin/env python3
"""Local read-only telemetry smoke. Requires running API, worker and compose observability.
No login, business mutation, personal data or external delivery. Never prints credentials/log bodies.
"""
import base64,json,os,time,urllib.request,urllib.parse,urllib.error
api='http://localhost:'+os.environ.get('API_PORT','5005')
prom='http://localhost:'+os.environ.get('PROMETHEUS_PORT','19090')
loki='http://localhost:'+os.environ.get('LOKI_PORT','13100')
grafana='http://localhost:'+os.environ.get('GRAFANA_PORT','3002')
def get(url,headers=None):
 with urllib.request.urlopen(urllib.request.Request(url,headers=headers or {}),timeout=5) as r:return json.load(r)
def wait(check,description):
 last=None
 for _ in range(30):
  try:
   if check(): print('[TEST] '+description+' — PASS',flush=True);return
  except (urllib.error.URLError,ValueError,KeyError,AssertionError) as e:last=type(e).__name__
  time.sleep(2)
 raise AssertionError(description+' failed ('+str(last)+')')
start=time.time_ns();marker='bf10-secret-not-for-telemetry'
for path in ['/health/live','/api/v1/status','/'+marker+'?password='+marker]:
 try:
  with urllib.request.urlopen(api+path,timeout=5):pass
 except urllib.error.HTTPError as e:assert e.code in (401,404)
def query(expression):return get(prom+'/api/v1/query?'+urllib.parse.urlencode({'query':expression}))['data']['result']
wait(lambda: bool(query('buildingflow_http_requests_total{status="401"}')),'API401 counter reaches Prometheus')
wait(lambda: bool(query('buildingflow_http_duration_seconds_bucket')),'Request duration histogram reaches Prometheus')
wait(lambda: bool(query('buildingflow_worker_messages_total')),'Worker outbox counter reaches Prometheus')
wait(lambda: len(query('probe_success{job="tcp-health"} == 1')) == 3,'PostgreSQL/Redis/RabbitMQ reachability series exist')
metrics=get(prom+'/api/v1/series?'+urllib.parse.urlencode({'match[]':'{__name__=~"buildingflow_.*"}'}))['data']
assert marker not in json.dumps(metrics)
assert all(not any(k in s for k in ['userId','personId','propertyId','url','query','request_body','Authorization']) for s in metrics)
print('[TEST] Bounded metric labels and sensitive query exclusion — PASS',flush=True)
credentials=os.environ.get('GRAFANA_ADMIN_USER','buildingflow-admin')+':'+os.environ.get('GRAFANA_ADMIN_PASSWORD','')
headers={'Authorization':'Basic '+base64.b64encode(credentials.encode()).decode()}
for uid in ['buildingflow-prometheus','buildingflow-loki']:
 wait(lambda uid=uid: get(grafana+'/api/datasources/uid/'+uid+'/health',headers).get('status')=='OK','Grafana '+uid+' datasource')
dashboard=get(grafana+'/api/dashboards/uid/buildingflow-operations',headers)['dashboard'];assert len(dashboard['panels'])==8
print('[TEST] Provisioned operations dashboard (8 panels) — PASS',flush=True)
def logs(service):return get(loki+'/loki/api/v1/query_range?'+urllib.parse.urlencode({'query':'{service_name="'+service+'"}','start':str(start),'end':str(time.time_ns()),'limit':'500'}))['data']['result']
wait(lambda: bool(logs('buildingflow-api')),'API structured logs reach Loki')
# An idle worker logs only at startup, which may be hours before this smoke; widen only the local time range.
start-=12*3600*1000000000
wait(lambda: bool(logs('buildingflow-worker')),'Worker structured logs reach Loki')
rows=logs('buildingflow-api');text=json.dumps(rows);assert marker not in text;assert 'trace_id' in text or 'TraceId' in text
print('[TEST] Loki trace correlation and sensitive query exclusion — PASS',flush=True)
