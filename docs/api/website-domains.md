# WEBSITE FAZ 07 — Alan adı, SEO ve yayınlama

19.10.2026 teknik teslim. Mevcut modular monolith ve public/admin ayrımı korunur. Canlı DNS, sertifika ve ters vekil kurulumu bu teslimde yapılmadı.

## Yönetim ve doğrulama

`/api/v1/properties/{propertyId}/website/domains`: GET liste, POST oluşturma; `/{id}/{verify,approve,primary,disable,rotate}` POST version ister; DELETE `/{id}?version=...` soft-delete eder. SUPER_ADMIN/SITE_ADMIN ve aktif alan üyeliği gerekir; custom domain onayı sadece SUPER_ADMIN içindir. UUID replay farklı içerikte 409; domain global tek, her sitenin tek primary kaydı vardır. IDN normalizasyonu uygulanır; IP, localhost, wildcard, URL ve port kabul edilmez.

Oluşturma/rotate yanıtı bir kez TXT değerini verir; liste/replay bu değeri geri vermez. DB sadece SHA256 saklar. DNS adı `_buildingflow-verify.{host}`; exact TXT değeri gerekir. Cloudflare DoH üzerinden bounded sorgu yapılır. HTTPS HEAD, geçerli CA/hostname ve yalnız public IP ister; DNS yanıtında özel IP varsa reddedilir, kontrol edilen IP bağlantıya sabitlenir, yönlendirme izlenmez. HTTP durum kodu TLS el sıkışmasından bağımsızdır. Custom onay, DNS ve TLS birlikte doğrulanmadan yayın yönlendirmesi açılmaz. Son doğrulama 24 saatten eskiyse public çözümleme kapanır; operatör yeniden verify çalıştırmalıdır. Otomatik sertifika üretimi/yenileme görevi eklenmedi.

`Website:PlatformDomain` platform alt alan adı sınırıdır. Gerçek DNS A/AAAA/CNAME kaydı, sertifika ve reverse proxy operatörce hazırlanır; proxy gerçek Host değerini korumalıdır. Güvenilir forwarded header sınırları mevcut [yayın prosedürüne](../RELEASE.md) tabidir. Sertifika/anahtarlar Git'e eklenmez. DNS sağlayıcısı protokolü: [Cloudflare DoH](https://developers.cloudflare.com/1.1.1.1/encryption/dns-over-https/make-api-requests/).

## Public yönlendirme ve SEO

GET `/api/public/v1/domains/{host}` sadece doğrulanmış, güncel, etkin domain ve aktif yayımlanmış siteyi çözer; diğerleri 404. GET `/api/public/v1/sites/{siteKey}/primary-domain` doğrulanmış canonical host döndürür. Özel veri/audit/token dönmez. Disable/rotate/askı sonraki istekte etkili olur; no-store korunduğu için paylaşılan cache/purge altyapısı eklenmedi.

Website `WEBSITE_PUBLIC_ORIGIN` güvenilir platform origin'idir. Custom Host backend eşlemesinden gelir; X-Forwarded-Host ve istemci iç başlıkları yönlendirme kaynağı olmaz. Bilinmeyen Host 404, çözümleme servisi arızası 503. Custom kök ilgili siteye rewrite edilir; başka tenant medya/form yolu açılmaz. Canonical doğrulanmış primary HTTPS host veya yapılandırılmış platform/siteKey yoludur. JSON-LD script içeriği escape edilir ve nonce CSP kullanır. Demo ve HTTP platform noindex; robots/sitemap yalnız yayımlanmış sayfaları ve ilan indeksini içerir. Tüm ilan detaylarının sitemap envanteri bu teslimde yoktur. Global platform sitemap tenant listesi ifşa etmez; site sitemap `/{siteKey}/sitemap.xml`, custom sitemap `/sitemap.xml`.

## Taslak önizlemesi

Yetkili POST `/api/v1/properties/{propertyId}/website/preview-token` site version alır. Sunucu `Website:PreviewHmacKey` Base64 en az32byte ister; yoksa kodlu409 ve kapalı davranış. Token site/version/nonce ve10dakika süreye bağlıdır. Site değişikliği eski tokenı geçersiz kılar. Anonim POST `/api/public/v1/preview` ve `/preview/media/{id}/{sha}` yalnız bu imzayla ve aynı site kapsamıyla veri verir; body max2048byte. Token URL, log veya localStorage'a konmaz.

Admin form POST ile sabit website `/preview` adresine geçer. Website yalnız `WEBSITE_MANAGEMENT_ORIGIN` origin'ini kabul eder; HttpOnly/SameSite=Lax, HTTPS'de Secure,600s cookie sonrası sabit `/preview/view` yönlendirmesi kullanılır. no-store/noindex uygulanır. Admin `NEXT_PUBLIC_WEBSITE_ORIGIN` CSP form-action için exact origin'dir; üretimde HTTPS kullanılır. Gerçek Keycloak tarayıcı kabulü yapılmadı. Yayın/geçmiş/restore mevcut immutable publication akışındadır.

## Migration ve kabul

Yeni `20261019065513_WebsiteDomains` migration/designer/snapshot; BaseEntity sekiz NOT NULL alanı, composite FK, global host/primary index ve durum kontrolleri. Dolu tabloda Down reddedilir; uygulanmış eski migration değiştirilmedi. Migration yalnız izole test DB'sinde uygulandı. Canlı uygulama mevcut [migration prosedürüyle](../MIGRATIONS.md) yükseltilmelidir.

Kanıt ve sınırlamalar [TESTING.md](../TESTING.md) WEBSITE FAZ07 bölümündedir. Yeni paket/font/ikon/görsel yok. FAZ06 gerçek EİDS/hukuki yayın kapısı açık kalır; FAZ08 başlamadı.
