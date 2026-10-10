# İş Fazı 07.1 — Merkezi API hata kodları ve süreli bildirimler

Onay: kullanıcının 2026-10-09 talebi. Bu altyapı ayrı olarak onaylandı; iş 07 Gider/Bütçe ve UI 07 Aidat/Muhasebe başlatılmaz. İş yol haritasına 07.1 eklenir; toplam 14 faz kaydı olur.

## Analiz ve karar

Mevcut ExceptionHandler 400/403/409/500 üretir; endpointlerdeki boş 404 ve authentication 401/403 yanıtlarında ortak kod yoktur. Beklenen doğrulama/çatışma metinleri Application ve Infrastructure içinde dağınıktır. Frontend known-problems metin allowlist'i ile TR/EN çevirir. NotificationProvider vardır; error/warning kalıcı, mobilde bildirim alttadır. MessageBanner ve FieldError hata metinlerini form/liste içinde gösterir.

Beklenen API hataları Application ErrorCatalog içinde sabit, anlamlı kod + güvenli mesaj + parametre tanımıyla tutulur. Domain'in iç invariant exception ayrıntıları ve beklenmeyen teknik hatalar kullanıcıya açılmaz; bunlar güvenli genel kod alır. Domain HTTP/UI bağımlılığı almaz. RequestValidationException yapılandırılmış alan sorunları, HierarchyConflictException kodlu mesaj taşır. String mesajdan kod türeten çalışma akışı kullanılmaz.

ProblemDetails korunur, genişletilir: status, title, code, message, parameters, severity; doğrulamada errors (eski uyumluluk) ve fieldIssues (kodlu alan sorunları). 404 bulunamayan/erişilemeyen kayıtta; 400 doğrulama/binding, 401 oturum, 403 izin, 409 çakışma/iş kuralı, 500 beklenmeyen hata olarak korunur. Uyarı işaretli 409 başarılı işlem değildir. Yetkisiz yanıtta kayıt varlığı veya hassas parametre açıklanmaz.

TR/EN sözlükler frontend'de kalır. Frontend yalnız tanımlı kodları ve o kodun izinli sayı/alan parametrelerini çevirir; ham server mesajını veya key/value'larını ekrana basmaz. Bilinmeyen/bozuk kodda HTTP durumuna göre güvenli çeviri. Eski API'nin metin sözleşmesi geçiş uyumluluğu olarak kalır; yeni kod önceliklidir.

Genel API, form ve uyarı metinleri ortak sağ üst bildirim alanında; error/warning ikon/semantik ayrımı, 8 saniye sonra kapanma, elle kapatma ve odak/hover sırasında süreyi durdurma. Eski inline genel hata metni tekrarlanmaz; retry/409 yenileme eylemleri ve formun aria-invalid/aria-describedby erişilebilir hata bağları korunur. Alan açıklamaları ekran okuyucuya ulaşır. Aynı mesajın yinelenmesi sınırlanır; iptal edilmiş GET istekleri bildirim oluşturmaz. Mobil de sağ üsttür; drawer'ın üzerinde ve yatay taşmasızdır.

## Beş resmi adım ve test planı

1. Mevcut sözleşme/mesaj envanteri ve bu analiz (tamamlandı; kaynak incelemesi).
2. Backend merkezi kod/parametre kataloğu, kodlu exception ve tüm API başarısız yanıtları.
3. Frontend kod bazlı TR/EN çeviri, güvenli parametre doğrulaması ve API istemcisi.
4. Ortak sağ üst error/warning bildirimleri, süre/iptal/dedup, form/liste geçişi.
5. Test, tarayıcı kabulü ve doğrulanmış roadmap/progress teslimi.

Backend: katalog tekilliği/parametre güvenliği; gerçek endpoint 400/401/403/404/409, malformed binding, 500 detay redaksiyonu; JWT/scope ve mevcut CRUD/finance regresyonları. Frontend: bütün kodların eş TR/EN karşılığı, ICU parametreleri, unknown/malformed/unsafe payload, severity/HTTP eşliği, eski yanıt, abort/dedup/süre. Native testler + typecheck/build. Tarayıcı: kaydetmeden yerel form hatası, kodlu 404/400, warning, kapanma/8 saniye, TR/EN, drawer ve 390 px. Kullanıcı finans/ilişki verisi mutate edilmez. Şema/migration/dep değişikliği yok; .env/lockfile/migration hashleri korunur. Test kanıtı TESTING.md; yalnız doğrulanan görev tiklenir.

## Teslim sonucu — 10.10.2026

**Beş resmi adım tamamlandı; 98 merkezi kod, API/field sözleşmesi, TR/EN ve sağ üst bildirim uygulandı.** Sonuç ve gerçek sınırlamalar [TESTING.md](../TESTING.md), protokol [STATUS.md](../progress/STATUS.md), sözleşme [errors.md](../api/errors.md). 252 .NET ve 59 FE testi, typecheck/build, 312 lisans, 19 korunan dosya ve TR/EN/390 px/404/warning/focus kabulü başarılı. Genel error/warning inline yerine toast; alan SR açıklaması ve retry/409 eylemi korunur. İş 07 / UI 07 ayrı ve bekliyor.
