# BuildingFlow yerel gözlemlenebilirlik — İŞ10

API/Worker → OpenTelemetry1.18.0 HTTP/Protobuf → Collector0.162.0; metrics→Prometheus3.15.0; logs→Loki3.7.8; Grafana12.4.11 görüntüleme katmanı. [Resmî Collector yapılandırması](https://opentelemetry.io/docs/collector/configuration/) ve [Loki native OTLP](https://grafana.com/docs/loki/latest/send-data/otel/) kullanıldı. Lisanslar [licenses.md](licenses.md).

## Çalıştırma

Proje kökünde:

```bash
docker compose --env-file .env -f deploy/docker-compose.yml up -d otel-collector prometheus blackbox loki grafana
bash scripts/dev.sh api
```

API ayrı terminalde. Worker isteğe bağlı üçüncü terminal:

```bash
bash scripts/dev.sh worker
```

Yerel worker teslim kanalı yapılandırılmazsa mail göndermez; compose worker yerel Mailpit'e gönderir. Outbox tablosu için migrations önceden uygulanmalı (`bash scripts/db.sh update`). `scripts/dev.sh infrastructure` bu servisleri de açar; önceki hazırlık davranışı API/FE container'larını durdurabilir, zaten açık geliştirmede yukarıdaki hedefli compose komutunu kullanın.

| Yerel adres | Amaç |
|---|---|
|http://localhost:3002/d/buildingflow-operations|Grafana8 panelli dashboard; mevcut .env admin bilgileri|
|http://localhost:19090|Prometheus query/targets|
|http://localhost:13100/ready|Loki readiness; sorgular Grafana Explore'dan|
|http://localhost:14318|Collector OTLP HTTP alıcısı; tarayıcı sayfası değildir|

Portlar .env.example'daki OTLP_PORT/PROMETHEUS_PORT/LOKI_PORT ile override edilir; varsayılanlar bilgisayardaki TraceFlow3100'ü kullanmaz. Tüm yayınlanan portlar loopback. Collector/Prometheus/Loki kimliksiz iç servislerdir; internete açılmaz. API üzerinde anonim `/metrics` endpointi eklenmedi. Docker içi datasource adresleri container isimleri/standart portları kullanır.

## Ölçümler ve loglar

`buildingflow_http_requests_total`: code-owned route template, GET/POST/PUT/DELETE/OPTIONS/HEAD/OTHER ve HTTP status. Ham URL/UUID/query etiketi yok. `buildingflow_http_duration_seconds_bucket` histogram; Grafana p95. Worker messages/failures/deliveries; outcome processed/duplicate/retry/dead_letter ve bounded relay/consumer/delivery operation.

Blackbox: API /health, Keycloak /health/ready, MinIO /health/live HTTP; PostgreSQL/Redis/RabbitMQ TCP erişimi. TCP probe kimlik bilgisi/işlem doğrulaması değildir. Docker API8080 ve yerel API5005 ayrı hedeflerdir; kullanılmayan çalıştırma biçiminin probe'u0 olabilir. Özel API_PORT kullanımında prometheus.yml yerel hedefi güncelleyin. Üretim alarm/replication/retention/backups İŞ11 kapsamında ayrıca değerlendirilir.

OTLP log provider yalnız BuildingFlow.* kategorisini Information+ aktarır. Framework request URL, EF SQL/body/auth başlıkları aktarılmaz. API olayında method/route/status/süre; hata olayında exception türü/status (mesaj/stack yok). Activity.TraceId/SpanId metadata ile Grafana Loki'de correlation. Worker olayları sayılar veya izinli işlem türleri; e-posta adresi, mesaj body veya credential yok. Console JSON trace scope'u korur; üretimde mevcut framework log seviye/politikası ayrı gözden geçirilmeli.

Trace ActivitySource `BuildingFlow.Operations`; manuel güvenli span'lar HTTP/outbox/consume. `Telemetry:ExportTraces=false` varsayılan; true olursa Collector debug/basic hedefine gönderilir. Tempo depolaması ve tam RabbitMQ traceparent propagation bu teslimde yok; uçtan uca trace deposu/dashboards varmış gibi gösterilmez. Log correlation çalışır. Yerel Loki7 gün retention, Prometheus7 gün; kalıcı volume'ları silmeyin.

Geliştirmede `BUILDINGFLOW_TELEMETRY_ENABLED=false` aktarımı kapatır. `Telemetry:Endpoint` credential/query/path içermeyen OTLP HTTP base URI olmalı. API/Worker compose endpointi http://otel-collector:4318; host endpoint14318.

## Smoke testi

API+worker+servisler çalışırken .env'yi paylaşmadan:

```bash
set -a
source .env
set +a
python3 scripts/test-observability.py
```

Salt-okunur health/401/404 istekleri; sayılar/histogram/worker,3 TCP probe, Grafana datasource/dashboard, Loki API+worker/trace_id ve hassas sorgu marker yokluğu. API/Broker iş verisi mutasyonu veya gerçek mail yok; parola/log body yazdırılmaz. Ayrıntı [TESTING.md](TESTING.md).
