# TEDARİKÇİ FAZ10 — Bildirimler, performans, raporlar

30.10.2026; dal `buildingflow_tedarikci`; [GitHub #100](https://github.com/AkaBayraK/BuildingFlow/issues/100). Plan FAZ10, kayıt11/13, resmi3/3=%100,00. Tedarikçi akışı11/13=%84,62. Sonraki FAZ11 (opsiyonel/onaylı AkaAI).

**Sözleşme bitişi, iş emri gecikmesi, bakım uyarıları; tercih/izin kontrolleri.** Uyarılar okuma anında canlı kayıtlardan hesaplanır: hatırlatma penceresine giren aktif sözleşmeler, SLA'sı geçmiş açık iş emirleri, yaklaşan ve geciken bakım tarihleri. Kullanıcının tenant PropertyScope'u ve tedarikçinin aktif alan bağlantısı dışındaki kayıt gösterilmez. Her kullanıcı (issuer+subject) üç uyarı türünü ayrı açıp kapatır; tercih version kontrollüdür. Gönderim kanalı eklenmedi: uyarılar web "Uyarılar" görünümündedir, e-posta/SMS/push yoktur.

**Tamamlanma süresi, zamanında tamamlama, maliyet ve puanlama; örneklem sayısı.** Seçilen dönem (≤366 gün) ve isteğe bağlı alan için tedarikçi başına: tamamlanan iş emri, atama→tamamlanma ortalama saat, SLA zamanında/örnek, bakım zamanında/örnek, zamanında oranı, onaylı/ödenen/mutabık fatura sayısı ve toplamı. Puan zamanında oranıdır ve yalnız en az 3 örnek varsa gösterilir; aksi halde "Yetersiz örnek" ve n her zaman görünür.

**Filtreli PDF/Excel export, veri erişimi ve formül enjeksiyonu koruması.** Uyarı ve performans görünümleri ekrandaki filtreyle Excel'de açılan UTF-8 BOM'lu CSV indirir; `= + - @` ile başlayan sayı olmayan hücreler `'` ile etkisizleştirilir. PDF tarayıcının yazdır→PDF'i ile, menü ve filtreler yazdırmada gizli. `.xlsx` üretilmez, yeni paket yok. Export yalnız kullanıcının API'den yetkiyle aldığı satırları içerir.

| Bu işte çalıştırılan doğrulama | Sonuç |
| --- | --- |
| Tam backend regresyonu | **Integration934/936 + Unit472/472**, 2 opt-in atlandı; 2 sınıfın temizliği paylaşılan PostgreSQL'de düştü (exit1) |
| O iki sınıfın tekrar koşumu | **26/26, exit0** |
| FAZ10 API + migration | **3/3** |
| Web test + typecheck + build | **343/343, GEÇTİ, GEÇTİ** |
| Mobil test | **170/170** |
| Kapsam kapısı / lisans | **GEÇTİ** (API 464) / **329 PASS** |

Loglar `/tmp/bf-sup10-{a,b,full,rerun,webbuild}.log`. Gerçek tarayıcıda ekran ve yazdırma görünümü kontrolü yapılmadı; migration yerel veritabanına uygulanmadı. Uyarılar tür başına ilk 500 kayıtla sınırlıdır (aşılırsa ekranda uyarı).

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL.
