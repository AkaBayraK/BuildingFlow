# Agent görev ayrımı
- **Orchestrator / Architect:** faz planı, repo analizi, bağımlılıklar, küçük diff ve task gate.
- **AkaAI Planner:** Türkçe istemi kısıtlı, doğrulanabilir senaryo planına dönüştürür; kod/DB doğrudan çalıştırmaz.
- **Web QA Agent:** Playwright, tüm Next.js ekranları, CRUD, accessibility, responsive, auth.
- **Mobile QA Agent:** Maestro veya repo ile uyumlu araç, iOS/Android, SQLite offline-first, çoklu rol/daire, online ödeme.
- **Finance QA Agent:** tahakkuk, borç, ödeme, webhook, idempotency, muhasebe, mutabakat.
- **Security QA Agent:** Keycloak, RBAC, tenant izolasyonu, KVKK, maskelenmiş log, OWASP.
- **Evidence & Reporting Agent:** screenshot, video, traces, API/DB assertions, rapor ve GitHub issue.
- **Data Steward:** sentetik demo/test verisi, deterministik seed, dry-run ve güvenli cleanup.

## Her ajan için sözleşme
Girdi: açık onaylı kapsam + ortam + run_id + izin verilen araçlar. Çıktı: yapılandırılmış test adımları, gerçek test sonucu, kanıt linkleri, fail nedenleri ve eksik kapsam. Kanıtsız PASS yazma. Kullanıcı onayı olmadan production, ödeme, dış servis, veri silme, issue kapatma veya otomatik düzeltme yok. Ortak method/fixture tekrarını önle. Her ajan Türkçe konsol raporu verir.

## GitHub task kapanış kontrolü
- [ ] Gereksinimler ve ekran kapsamı tamamlandı
- [ ] Kod review / lint / build / unit / integration / E2E PASS
- [ ] Web + mobil ilgili platform testleri PASS veya gerekçeli kapsam dışı (önceden onaylı)
- [ ] Güvenlik/KVKK/lisans kontrolleri PASS
- [ ] Kanıtlar, log/trace ve rapor issue'ya eklendi
- [ ] Hata issue'ları çözüldü ve tekrar test edildi
- [ ] Test veri temizliği veya retention durumu doğrulandı
- [ ] Kullanıcı onayı gerektiren kararlar kapatıldı
Yalnızca bütün zorunlu kutular tamamlanırsa GitHub issue kapat; BLOCKED/SKIPPED/FAIL olan zorunlu test varken kapatma.
