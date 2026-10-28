# Test kapsam matrisi — zorunlu minimum
Her satır için web/mobil uygulanabilirliği, rol, tenant, pozitif/negatif senaryo, UI/API/DB kanıtı ve run_id kaydı tutulur. Gerçek repo envanteriyle genişlet.

| Alan | Zorunlu senaryolar |
|---|---|
| Tüm CRUD ekranları | Create, read, update, delete/soft-delete, liste, arama, filtre, sıralama, sayfalama, zorunlu alan, duplicate, yetkisiz erişim, audit |
| Yönetim alanları | Tek bina/site/blok, çoklu tenant, kayıt/ilişki doğrulama |
| Kişiler ve daireler | Malik/kiracı/sakin, birden fazla alan/daire, rol değiştirme, ilişki silme |
| Aidat | Parametre, dönem, tahakkuk, mükerrer engelleme, doğru borçlu, bakiye |
| Web ödeme | Başlat, sandbox, 3DS, sonuç, webhook, makbuz, muhasebe, mutabakat |
| Mobil ödeme | iOS/Android, online zorunluluğu, bağlantı kopması, belirsiz ödeme, retry |
| Hata senaryoları | Decline, timeout, duplicate tap, duplicate webhook, out-of-order webhook, iade/iptal, kısmi ödeme |
| Mobil offline | SQLite, queue/sync, çakışma, yeniden bağlanma, tenant izolasyonu |
| Duyuru/anket | Yayın, görünürlük, cevap, tarih/rol/tenant kontrolleri |
| Talep/arıza/bakım | Oluşturma, atama, durum geçişleri, bildirim, sonuç |
| Araç/otopark | Kayıt, eşleme, kapasite, yetki, silme |
| İlan/web sitesi | Malik yetkisi, dış yönlendirme, soft-delete, kamuya açık görünüm |
| Rapor/export | PDF/Excel, filtre, toplam ve finansal doğruluk |
| Güvenlik | Keycloak, RBAC, IDOR, KVKK, log masking, rate-limit, audit |
| Performans | Eşzamanlı borç/ödeme, p95, kaynak kullanımı, darboğaz |
| Veri temizliği | Test/demo ayrımı, dry-run, silme onayı, prod guard, audit koruma |
| AI Agent | Prompt planı, schema, onay, tool allowlist, injection, hallucination, rapor kanıtı |

## Kanıt formatı
`{run_id, phase, module, screen, scenario, step, actor_role, tenant, platform, started_at_utc, duration_ms, expected, actual, status, screenshot_uri, video_uri, trace_id, request_id, masked_logs_uri, issue_url}`

## Örnek doğal dil promptları
1. `Bütün BuildingFlow ekranlarını envantere göre gez; CRUD, arama, filtre, doğrulama, rol ve hata senaryolarını çalıştır; ekran ekran maddeli rapor üret.`
2. `A Blok A-12 kiracısı için 2.500 TL aidat oluştur, mobil iOS sandbox ödemesi yap, webhook'u geciktir, iki kez tahsilat olmadığını ve borç/muhasebe tutarlılığını doğrula.`
3. `Malik birden fazla yönetim alanındaki dairelerini web ve mobilde görebiliyor mu; kiracının yetkisiz daireye erişimini engelle ve kanıtla.`
4. `Sentetik demo verilerini oluştur, bütün ilgili ekranlarda göster, dry-run temizleme raporu hazırla; silmeden önce onayımı bekle.`
