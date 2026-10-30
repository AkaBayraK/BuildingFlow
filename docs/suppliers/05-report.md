# TEDARİKÇİ FAZ05 — Teklif ve karşılaştırma

30.10.2026; dal `buildingflow_tedarikci`; [GitHub #95](https://github.com/AkaBayraK/BuildingFlow/issues/95). Plan FAZ05, kayıt06/13, resmi2/2=%100,00. Tedarikçi akışı6/13=%46,15. Önceden onaylanan sıra korunur; sonraki FAZ06'dır.

**Teklif talebi/çoklu teklif/KDV/para birimi/tarih/karşılaştırma/gerekçeli karar ve audit/rol ayrımı/taslak iş emri dönüşümü uygulanıp doğrulandı.** Alan ve kategoriye bağlı1–20 açık davet, supplier başına tek hizmet, net/KDV/brüt ve TRY/USD/EUR ayrı karşılaştırma vardır. Kur veya otomatik seçim yoktur. Talep ve geçerlilik günü Europe/Istanbul bugün dahil, KDV girişi hukuki oran kataloğu değildir.

**SITE_ADMIN/SUPER_ADMIN kararında gerçek alan yetkisi ve gerekçe zorunlu; ilk giren kendi teklifini onaylayamaz.** Yalnız ilk giren PENDING teklifini düzenler; başka kişi düzenleyip bu ayrımı aşamaz. Onay/red değişmez. Tek onay talebi AWARDED yapar; eski UUID replay güncel değer/geçmişi döndürür. Normalize tutar ölçeği ve davet sırası aynı fingerprint'i üretir. Tenant→Property kilidi, version ve eşzamanlı yazım409 korunur.

**Onaylı tekliften tek gerçek SupplierWorkOrder DRAFT oluşur.** Tedarikçi/hizmet/alan/brüt/para birimi snapshot korunur. İş emri yürütme/arıza bağlantısı FAZ06; ödeme/bildirim başlatılmaz. Beş yeni BaseEntity tablo, composite FK/kategori kontrolü, değişmez geçmiş/nihai veri trigger'ları ve veri varken Down koruması `20261029180412_SupplierQuotes` içinde; snapshot eşleşir. Canlıya uygulanmadı.

Değişen kaynaklar: `SupplierQuote.cs`, Application `SupplierQuotes.cs`, `SupplierQuoteConfigurations.cs`, `SupplierStore.Quotes.cs`, `SupplierQuoteEndpoints.cs`, DI/Program/HierarchyStore/ErrorCatalog; FE `quotes-model.ts`, `SupplierQuotes.tsx`, SupplierPanel/style, TR/EN ve error registry; unit/integration/migration/web testleri. Önceki supplier kaynakları çalışma ağacında korunur. Sağ panel için yerel grid genişlik sınırı eklendi; global header stilinin teklif özetine yan etkisi kapsayıcı değişimiyle giderildi.

| Bu işte çalıştırılan doğrulama | Sonuç |
| --- | --- |
| 29.10 geniş supplier/communication/migration/property/error/release regresyonu | **142/142 integration +30/30 unit, atlanan0** |
| Normalize UUID ve tarih sınırı sonrası supplier/error | **32/32 integration +21/21 unit, atlanan0** |
| 30.10 son güvenlik commit'iyle supplier/release/error | **63/63 integration +21/21 unit, atlanan0** |
| Son web sözleşme/i18n/negatif testleri | **322/322, atlanan0** |
| Typecheck ve son production build | **GEÇTİ** |
| EF pending-model kontrolü | **Model/snapshot aynı** |
| Lisans kontrolü | **329 PASS**, yeni paket yok |
| `git diff --check` | **GEÇTİ** |

PostgreSQL testleri ayrı geçici DB/rol/şemada çalıştı; yalnız kendi kaynakları temizlendi. İlk kurulum/yükseltme/tekrar uygulama/boş rollback/dolu rollback, beş tablonun40 zorunlu NOT NULL kolonu, yanlış kategori bağlantısı ve immutable SQL negatifleri doğrulandı. Yarışta tek yazım başarılı, diğeri409; kendi onayı SUPER_ADMIN için de403; ACCOUNTANT/anonim/yabancı alan/kaldırılmış üyelik negatifleri geçer.

Tarayıcı: gerçek React bileşenleri + sentetik loopback API. Boş formun ilk hatalı alana odağı, iki davet oluşturma, TR12000,50→KDV2400,10/brüt14400,60, EUR500→600, kendi onayının kapalı olması, farklı aktörle gerekçeli red/onay, tek DRAFT dönüşüm ve altı adımlı geçmiş görüldü. Son CSS kontrolünde önceden tanımlı aynı sentetik örnek kullanıldı; canlı API/DB E2E diye sayılmaz. Masaüstünde panel510/510;375px TR/EN koyuda373/373 ve sayfa375;768px tablette612/612 ve sayfa768: **yatay taşma0**. Geçici viewport geri alındı.

Kanıtlar: [karşılaştırma](evidence/05-currency-comparison.png), [iş emri ve audit](evidence/05-draft-work-order-history.png), [375px EN koyu](evidence/05-375-en-dark.png). Loglar `/tmp/bf-supplier05-{regression,final-tests,security-final,delivery-webtests,delivery-typecheck,release-webbuild,model,licenses}.log`.

Gerçek OIDC/API tarayıcı E2E, mobil/native, gerçek tedarikçi portalı, dış bildirim, yük/pentest/deploy bu fazda çalıştırılmadı. Yerel API yeniden başlatılmadı, kullanıcı verisi/.env değiştirilmedi; supplier commit/push yok. Aynı dalda başka çalışmadan gelen QA ve güvenlik commitleri korundu; dışarıya itilmedi. Kaynak `docs/api/suppliers.md`; toplu uçtan uca yayın kabulü FAZ12'dedir.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL. Hesap kota yüzdesi bu sohbetin token tüketimi değildir.

Ürün yol haritası:37 kayıt,157/168=%93,45. Codex usage/rateLimitsByLimitId.codex30.10.2026 okuması:300dk kullanılan%5/kalan%95,reset30.10.2026 13:12:31;10080dk kullanılan%63/kalan%37,reset04.11.2026 11:43:32 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.
