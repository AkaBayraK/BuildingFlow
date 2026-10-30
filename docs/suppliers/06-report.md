# TEDARİKÇİ FAZ06 — İş emirleri ve Talep/Arıza bağlantısı

30.10.2026; dal `buildingflow_tedarikci`; [GitHub #96](https://github.com/AkaBayraK/BuildingFlow/issues/96). Plan FAZ06, kayıt07/13, resmi3/3=%100,00. Tedarikçi akışı7/13=%53,85. Sonraki FAZ07'dir.

**Mevcut talep/arıza (MobileRequest) ve bakım planı (MaintenancePlan) kaydından iş emri oluşturulur.** Kaynak aynı tenant/Property ve beklenen version ile bağlanır, kaynak metni anlık görüntü olarak saklanır; bir kaynakta tek açık iş emri vardır. Kaynak kaydın durumu otomatik değiştirilmez. FAZ05 teklif DRAFT iş emirleri migration ile korunur.

**Durum makinesi:** DRAFT→ASSIGNED→ACCEPTED→IN_PROGRESS→COMPLETED→VERIFIED→CLOSED; ASSIGNED'dan gerekçeli REJECTED, kapanış öncesi gerekçeli CANCELLED. Atama SLA ister; SLA UTC saklanır, Europe/Istanbul gösterilir, açık iş emrinde gecikme göstergesi vardır. Kabul/başlatma iç yönetici kaydıdır, dış tedarikçi portalı yoktur. Doğrulama ve kapanış yalnız alan yetkili SITE_ADMIN/SUPER_ADMIN; MANAGER 403 alır.

**Fotoğraf kanıtı:** JPEG/PNG imza+MIME+uzantı, en fazla10, COMPLETED sonrası yükleme yok; mevcut özel supplier belge deposu, JWT kontrollü `no-store` indirme ve SHA doğrulaması. Fotoğraf kanıtı kaldırılamaz, genel Property belge ucundan erişilemez.

**Eşzamanlılık ve idempotency:** tenant→Property kilidi, beklenen version, normalize UUID fingerprint; aynı UUID replay güncel kaydı ve ilk appliedVersion'ı döndürür, farklı içerik409. Eşzamanlı iki geçişte biri200 diğeri409. Immutable revizyon geçmişi, composite FK ve dolu Down guard `20261030052649_SupplierWorkOrders` içinde. Canlıya uygulanmadı.

**Kapsam kapısı bakımı:** Kalite kapısı route metnini testlerde düz metin olarak arar; supplier testleri URL'yi parça birleştirdiği için40 endpoint testsiz görünüyordu. Testler tam route metniyle yazıldı (davranış aynı). Ana daldaki varsayılan "giriş zorunlu" değişikliğiyle sayıma giren üç anonim public okuma (ilan listesi, form challenge, birincil alan adı) için HTTP testi eklendi. Kapı GEÇTİ; taban230→190 endpoint küçüldü.

| Bu işte çalıştırılan doğrulama | Sonuç |
| --- | --- |
| Tam backend regresyonu (`scripts/test.sh`) | **Integration922/924 (2 opt-in canlı OIDC atlandı) + Unit464/464** |
| Supplier+Listing+WebsiteDomain+endpoint envanteri | **59/59 + 93/93** |
| Anonim endpoint allowlist testi (`QaEndpointInventoryTests`) | **GEÇTİ** |
| Kapsam kapısı (`qa_coverage_gate.py`) | **GEÇTİ, yeni testsiz kalem0** |
| Web test + typecheck | **330/330, GEÇTİ** |
| Web production build | **GEÇTİ** |
| Lisans kontrolü | **329 PASS**, yeni paket yok |

Loglar `/tmp/bf-sup06-{a,b,full,web,webbuild}.log`. PostgreSQL testleri ayrı geçici DB/rolde çalıştı ve temizlendi.

Bu fazda gerçek tarayıcıda UI ekran kontrolü yapılmadı; bileşenlerde busy/409/odak davranışı kodda mevcut, web sözleşme testleri geçti. Canlı OIDC/API tarayıcı E2E, mobil, gerçek bildirim, deploy yok; yerel DB'ye migration uygulanmadı. Toplu uçtan uca kabul FAZ12'dedir.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL.
