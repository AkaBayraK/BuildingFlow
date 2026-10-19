# WEBSITE FAZ 08 — Güvenli giriş ve site bağlamı

19.10.2026 teknik teslim. Yeni kimlik deposu, parola formu, rol veya bağımlılık eklenmedi.

## Kullanıcı akışı

Public sitedeki tek Hesabıma Giriş bağlantısı yalnız sunucuda yapılandırılan `WEBSITE_MANAGEMENT_ORIGIN` adresine gider. HTTPS zorunludur; yerel geliştirmede localhost/127.0.0.1 HTTP kabul edilir. Kullanıcı bilgisi, path/query/hash içeren origin reddedilir. Ortak bağlantı `/website-entry/{siteKey}/all` açar; `target="_blank" rel="noopener noreferrer"` ile giriş ayrı sekmededir. Hero içindeki tekrar eden portal kartı, footer ve mobil menüdeki tekrar linkleri kaldırılmıştır. Mobil üst menüde aynı tek bağlantı kalır. LOGIN_CARD snapshot bölümü tarihçe uyumu için saklanır fakat premium render içinde gösterilmez. Önceki management/resident deep linkleri uyumluluk için çalışmaya devam eder. Kök şablon önizlemesi gerçek bir site kaydı olmadığı için genel yönetim köküne gider; hayali demo site context’i aranmaz. Gerçek tenant sitelerinde site/all kapsamı korunur. Demo site gerçek hesap veya örnek yetki oluşturmaz.

Bu rota mevcut yönetim uygulamasının AuthGate bileşeni içindedir: Keycloak Authorization Code + S256 PKCE, bellek içi token ve aynı origin kök callback kullanılır. Public uygulama parola/token almaz. SessionStorage sadece exact allowlist yerel dönüş path'ini tutar; harici URL, query, fragment, ek path, encoded traversal ve farklı audience kabul edilmez. Logout bekleyen dönüş path'ini temizler. Giriş audience değeri rol atamaz.

Girişten sonra kullanıcı, o sitenin backend tarafından yetkilendirilen çalışma alanını seçer. Ortak all audience, aynı sitedeki güncel PLATFORM/MANAGEMENT/OWNER/TENANT/RESIDENT bağlamlarını birlikte gösterir; kullanıcı girişten önce rol seçmez. Eski management yalnız PLATFORM/MANAGEMENT; eski resident yalnız OWNER/TENANT/RESIDENT kayıtlarını gösterir. Birden fazla ilişkinin kimlikleri korunur. Genel `/workspaces` ekranı mevcut tüm yetkili bağlamları sunmaya devam eder; public site linki hesabın başka alanlardaki meşru yetkisini kaldırmaz.

## Authenticated API

GET `/api/v2/session/contexts/website/{siteKey}/{audience}?page=1&pageSize=20`

- JWT zorunlu; anonim/bozuk JWT 401. Audience `all`, `management` veya `resident`; public CTA yalnız `all` kullanır.
- Mevcut `SessionContextStore.Entries` güvenlik modeli tekrar kullanılır. Realm rolü, aktif görev/üyelik, issuer+subject hesap bağı ve güncel tarihli malik/kiracı/sakin ilişkisi backend'de belirlenir. SUPER_ADMIN platform istisnası management bağlamındadır; sakin ilişkisi icat etmez.
- Site etkin, yayımlanmış ve aktif publication sahibi olmalıdır; Property etkin olmalıdır. Yalnız aynı Property'deki güncel bağlamlar döner. Site yok/askıda/taslak veya hesap için eşleşen bağlam yoksa aynı 404; varlık bilgisi ayrıştırılmaz.
- `page` 1–100000, `pageSize` 1–100; mevcut `PagedRows<SessionContext>` sözleşmesi. Geçersiz sayfalama 400. RepeatableRead ile site/bağlam listesi tutarlı okunur; no-store uygulanır.
- Bağlam kimliği erişim yetkisi değildir. Detay ve her işlem mevcut backend yetkilendirmesini tekrar yapar. İptal sonraki API isteğinde etkili olur. UI yenileme/yeniden deneme ve sekmeye odak dönüşünde veriyi yeniler; hesap/site/audience değişiminde önceki kartlar temizlenir ve önceki istek iptal edilir.

## Ortam ve kabul

Bu akış mevcut merkezi kişi/çalışma alanı özellik kapılarına bağlıdır: API `PersonRefactor:Enabled` ve `PersonRefactor:SessionContextsEnabled`; yönetim FE `BUILDINGFLOW_SESSION_CONTEXTS_ENABLED=true`. Kapalı API 404; kapalı FE açıklayıcı bildirim gösterir. Bayraklar bu fazda gerçek ortamda değiştirilmedi. Açılmadan önce mevcut merkezi kişi, issuer/subject, görev, üyelik ve ilişki hazırlığı [session-workspaces.md](session-workspaces.md) kapsamında tamamlanmalıdır; eski rolü otomatik yeni yetkiye dönüştürme yoktur.

Keycloak'ın mevcut client redirect URI/web origin ayarları yönetim uygulamasını kapsamalıdır; public/custom website origin'i callback veya wildcard olarak eklenmez. 19.10.2026 BAKIM browser kontrolünde yeni sekmede gerçek Keycloak Code+S256 giriş ekranına ulaşıldı; kullanıcı bilgisi girilmedi. Kimlikli callback/çıkış/yenileme ve gerçek dağıtım kabulü yapılmadı. Mevcut hesap/parola/.env/servis korunur. Backend yeniden başlatılmadı; yeni endpoint için mevcut API koddan yeniden başlatılmalıdır. Migration yok.

Kanıt: [TESTING.md](../TESTING.md) WEBSITE FAZ08. FAZ06 EİDS/hukuki yayın engeli ve FAZ07 canlı DNS/TLS kabulü ayrı kalır. FAZ09 başlamadı.
