# WEBSITE FAZ09 — Teknik teslim ve kabul sınırı

19.10.2026; kanonik kaynak BuildingFlow-WebsiteManagement-Prompt-Paketi/PHASES.md. Kayıt10/10, iki resmi madde teknik2/2=%100,00. WEBSITE toplam9/10=%90,00; İŞ21 T21.9 dahil10/13=%76,92. FAZ06 EİDS/hukuki kabulü tamamlanmadığı için bütün WEBSITE işi100 sayılmaz. T21.6/T21.10/T21.12 açık. [FAZ09 #29](https://github.com/AkaBayraK/BuildingFlow/issues/29) teknik incelemede; [açık kabul #30](https://github.com/AkaBayraK/BuildingFlow/issues/30).

## Uygulananlar

- `scripts/check-website.sh`: mevcut bağımlılık bağlantısı,22website test/typecheck/build/HTTPsmoke ve CycloneDX üretimi. Mevcut CI'ya eklendi; diğer modüllerin kalite kapıları korunur.
- `website/scripts/release.mjs`: HTTPS ve exact ayrık origin üretim preflight; eksik/unsafe ayarlar fail-closed, değerler yazdırılmaz. Mevcut frontend lock + incelenmiş lisans envanterinden CycloneDX1.6;230npm kaydı. `release.test.mjs` pozitif/negatif4test.
- Standalone Next build, website Dockerfile, isteğe bağlı Compose website profili, loopback3004, nonroot USER node, `/health`204 ve Docker healthcheck. Runtime health API/DB hazır oluşu değildir. Admin Docker/CSP form-action için NEXT_PUBLIC_WEBSITE_ORIGIN arg/env aktarımı tamamlandı. .dockerignore nestedenv/privateconfig/artifact koruması genişledi.
- Docker imajında mevcut LICENSES/shadcn LICENSE/NOTICE, görsel kaynak kaydı, SBOM ve `bundle-notices.mjs` ile148kurulu kök bildirim kaydı korunur. 7kök dosya eksiği metadata manifestinde açık; tam OS/binary/ticari dağıtım kabulü değildir.
- `scripts/test-website-container.py`: UUID adlı ağsız/read-only/cap-drop/no-new-privileges konteyneri; health204/demo200/görsel200/CSP/bilinmeyenHost fail-closed/nonroot/SBOM/NOTICE/HTML<1MiB kontrolü; yalnız kendi konteynerini temizler.
- `scripts/dev.sh website`, güncel README/envexample, [işletim rehberi](operations.md), [yönetici rehberi](admin-guide.md). Yeni migration/entity veya üçüncü taraf paket eklenmedi.

## Bu fazda yeniden çalıştırılan kanıt

| Kontrol | Gerçek sonuç |
|---|---|
| İzole PG/storage API/yetki/JWT/migration/audit regresyonu |75/75integration,90/90seçili unit;0fail/skip; fixtureDB/rol/bucket temizlendi|
| Tüm backend unit |277/277;0fail/skip|
| Yönetim |161/161;typecheck/productionbuild PASS|
| Website |22/22;typecheck/standalone productionbuild/HTTPsmoke PASS|
| Website CI scripti |Baştan sona PASS; üretilen artifacts/website/npm.cdx.json|
| Docker build/runtime |Yerel phase09 test imajı; ağsız/read-only/nonroot kontroller PASS; test konteyneri temizlendi|
| Compose ve shell |profile website config --quiet ve bash -n PASS; servis başlatılmadı|
| npm audit |Paylaşılan frontend lock0açık; yalnız web/npm kapsamı|
| Lisans metadata |329npm/NuGet PASS; envanter içerik farkı yok|
| SBOM/NOTICE |230lock npm kaydı;148bildirim;7kök eksik dosya #30|
| Üretim origin preflight |Geçerli fixture PASS; eksik ortamFAILbeklendi; unsafe/missing/query/hash/userinfo/HTTP/sameorigin unit reddi geçti|
| CI sonuç kapısı |Mevcut6negatif/pozitif senaryo PASS|
| Tarayıcı |1280tekmain/tekh1/langtr/eksikalt0/isimsizgörünürlink0/kırıkimg0; Tab→İçeriğe atla,Enter→contentodak;390/320taşma0,tekCTA/yeni sekme hedefi|

Backend filtre WEBSITE07 kapsamına SessionContextTests ve AuthenticationTests eklenerek çalıştırıldı; komut TESTING.md içinde. Tüm backend entegrasyon suite, uzak GitHub Actions, mobil audit veya gerçek kullanıcı girişi bu sonuçlarla geçti sayılmaz. İlk Docker smoke Cache-Control eşitliği sadece no-store bekliyordu; gerçek daha kısıtlayıcı private,no-store için üyelik kontrolüne düzeltildi; son koşu başarılı. Docker npmci sonrasında host lisans kontrolü yeniden çalıştırıldı.

## Açık kabul ve korunmuş ortam

Gerçek EİDS/hukuki sağlayıcı, DNS/TLS/proxy/sertifika yenileme, tam kimlikli Keycloak callback/refresh/logout, gerçek yönetim alanı/telefon/ekran okuyucu, Lighthouse/CWV/yük ölçümü, tam SAST/containerOS/binary/dağıtım lisansı, hedef PostgreSQL backup/restore ve üretim rollout kabulü yapılmadı. #30 her maddeyi sahiplik/kanıt gereksinimiyle izler; fail-closed kapıları açılmadı. Mevcut mobil güvenlik kayıtları yeniden ölçülmedi.

Bu fazda gerçek .env/hesap/parola/rol/DB/API/Worker değişmedi; yeni paket/font/ikon/görsel yok. Mevcut kullanıcı çalışma değişiklikleri korundu. Yerel Docker build mevcut Node24Alpine tabanını kullanır; gerçek stack ayağa kaldırılmadı. Kod commit/push/deploy yapılmadı. GitHub görev gövdeleri ve proje durumları kullanıcı isteğiyle güncellendi; ayrı ürün fazı oluşturulmadı.
