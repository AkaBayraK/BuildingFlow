# TEDARİKÇİ FAZ08 — Muhasebe, fatura ve gider entegrasyonu

30.10.2026; dal `buildingflow_tedarikci`; [GitHub #98](https://github.com/AkaBayraK/BuildingFlow/issues/98). Plan FAZ08, kayıt09/13, resmi3/3=%100,00. Tedarikçi akışı9/13=%69,23. Sonraki FAZ09'dur.

**Alan bazlı fatura/gider ilişkilendirme ve mükerrer engeli.** Tedarikçi faturası tedarikçinin bağlı olduğu yönetim alanına kaydedilir; isteğe bağlı tamamlanmış iş emri, aktif sözleşme ve iptal edilmiş faturanın düzeltmesi bağlanır. Numara boşluk/nokta/tire/eğik çizgi ve harf büyüklüğü yok sayılarak aynı tedarikçide iptal edilmemiş faturalar arasında tekildir (uygulama + kısmi unique index). Onay, mevcut Gider modülüne `RequestId = faturaId` ile tek gider yazar; faturaya bağlı gider Gider ekranından ayrıca iptal edilemez.

**Onay, ödeme durumu, mutabakat, iptal/düzeltme audit ve finans rolleri.** RECORDED → APPROVED → PAID → RECONCILED; RECORDED/APPROVED gerekçeli CANCELLED (onaylıysa gider de iptal), ödenmiş fatura iptal edilemez, düzeltme tek yeni fatura ile. Kayıt tüm finans rolleri; onay/iptal yalnız alan yetkili SITE_ADMIN/SUPER_ADMIN ve kaydedenden farklı kişi; ödendi/mutabakat ACCOUNTANT veya yönetici; MANAGER onay ve ödeme yapamaz. UUID replay, version/tenant+Property kilidi, eşzamanlı onayda tek gider (200+409), immutable revizyon ve DB trigger.

**Gerçek para hareketi yok.** "Ödendi" yalnız sistem dışında yapılmış ödemenin tarih ve referans kaydıdır; yeni ödeme sağlayıcısı veya paket eklenmedi.

| Bu işte çalıştırılan doğrulama | Sonuç |
| --- | --- |
| Tam backend regresyonu | **Integration930/932 + Unit472/472**, 2 opt-in canlı OIDC atlandı |
| FAZ08 entegrasyon/migration + unit | **4/4 + 3/3** |
| Web test + typecheck + production build | **340/340, GEÇTİ, GEÇTİ** |
| Kapsam kapısı | **GEÇTİ**, taban 190→189 (Gider iptal ucu artık testli) |
| Lisans | **329 PASS**, yeni paket yok |

Loglar `/tmp/bf-sup08-{a,full,webbuild}.log`. Muhasebeci için ayrı fatura ekranı yok: web arayüzü tedarikçi detayındaki Faturalar sekmesi (yönetici rolleri); muhasebeci API'yi ve oluşan giderleri Gider modülünü kullanır. Gerçek tarayıcı UI ve canlı OIDC E2E yapılmadı; migration yerel veritabanına uygulanmadı.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL.
