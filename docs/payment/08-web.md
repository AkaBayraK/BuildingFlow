# BF-PAY FAZ 08 — Web yönetim ekranları

Tarih: 24.10.2026. Durum: **TAMAMLANDI (tarayıcı kabulü hariç, bkz. Açık riskler)**. Önceki faz çıktısı: backend ödeme API'leri (FAZ 02–07) derleniyor, `OnlinePayment` entegrasyon 9/9 ve unit 14/14 bu turda tekrar geçti.

## Görev listesi

| # | Görev | Durum |
|---|---|---|
| 1 | Önceki faz çıktısını doğrula (API, migration, testler) | TAMAMLANDI |
| 2 | Strict DTO okuyucuları, IBAN mod-97, ikon/ton eşlemeleri (`features/online-payments/model.ts`, `payment-style.ts`) | TAMAMLANDI |
| 3 | Yönetim alanı > Tahsilatlar > **Banka ve ödeme ayarları**: ayarlar, banka hesabı öner/onayla/reddet/kapat/varsayılan, çift onay, üye işyeri ekle/varsayılan/pasif, **bağlantı testi** | TAMAMLANDI |
| 4 | **Online ödemeler**: durum çipleri, tarih filtresi, özet, detay (dağılım, sağlayıcı olayları, iade, mutabakat), REVIEW dağıtımı, iade talebi + farklı yetkili onayı | TAMAMLANDI |
| 5 | **Defter** (ters kayıtlı, CSV) ve **Mutabakat** (günlük özet, kalemler, çözüm notu, elle çalıştırma) | TAMAMLANDI |
| 6 | Makbuz görüntüleme/yazdırma; manuel tahsilatta makbuz no, tahsilat hesabı seçimi, `ONLINE_CARD` yöntemi ve online tahsilatta iptal yerine iade yönlendirmesi | TAMAMLANDI |
| 7 | Sistem ayarları > **Ödeme sağlayıcıları** (yalnız SUPER_ADMIN) | TAMAMLANDI |
| 8 | TR/EN sözlük (235 anahtar + 7 ödeme anahtarı), hata kodları, erişilebilirlik | TAMAMLANDI |
| 9 | Testler: `tests/online-payments.test.mjs` 10/10, web 260/260, typecheck | TAMAMLANDI |
| 10 | Gerçek API/izole PG ile tarayıcı kabulü | BEKLIYOR |

Kapsam kararı: ayrı menü yerine mevcut Tahsilatlar modülüne sekme olarak eklendi (Aidat modülündeki gecikme sekmeleri kalıbı); yeni route/OIDC whitelist değişikliği gerekmedi.

## Değişen dosyalar

- Yeni: `frontend/buildingflow-web/src/features/online-payments/{model.ts,payment-style.ts,PaymentSettingsPanel.tsx,OnlinePaymentsPanel.tsx,LedgerPanels.tsx,ProvidersPanel.tsx}`, `tests/online-payments.test.mjs`.
- Değişen: `features/payments/{PaymentsWorkspace.tsx,PaymentDialogs.tsx,model.ts}`, `app/(workspace)/system-settings/page.tsx`, `messages/{tr,en}.json`.
- Backend/migration değişmedi. Yeni paket yok; `npm ci` aynı lock ile, lisans kontrolü 329/329 PASS.

## Güvenlik ve izolasyon

- Tüm çağrılar `/api/v1/properties/{id}/...` altında; yetki ve üyelik backend'de. UI rol kontrolü yalnız düğme görünürlüğü (öner: SUPER_ADMIN/SITE_ADMIN/MANAGER, onay: SUPER_ADMIN/SITE_ADMIN; çift onayda öneren onaylayamaz).
- Gizli anahtar girilmez; yalnız referans adı (`^[A-Za-z0-9_:-]{1,100}$`). Ortam yalnız SANDBOX; LIVE okuyucuda reddedilir.
- Kart verisi, localStorage ve konsol logu yok (testle kontrol). Komisyon yalnız ABSORBED gösterilir.
- Mutation'lar UUID + aynı içerik için aynı istek kimliği; kör retry yok; 409'da güncel kayıt yeniden okunur.

## Testler

- Web: `npm test` **260/260**, `npx tsc --noEmit` geçti. Yeni dosya 10/10.
- Backend (ayrı geçici PostgreSQL, `scripts/test-payments.py OnlinePayment`): entegrasyon **9/9**, unit **14/14**.
- `npm run build` **geçmedi**: hata bu fazdan değil; eşzamanlı iletişim doğrulama çalışması `features/account/contact-verification.ts` dosyasında Turbopack kökü dışındaki `../../../../../shared/` klasörünü içe aktarıyor. Bu fazın dosyaları typecheck'ten geçiyor.

## Açık riskler

- Tarayıcıda gerçek API ile ekran kabulü yapılmadı (Keycloak + API + PG yığını bu turda kurulmadı).
- Production build, iletişim doğrulama çalışmasındaki `shared/` içe aktarımı düzelene kadar kırık.
- FAZ 02–07 için ayrı faz raporu bulunmuyor; backend kanıtı testlerdir.

## Geri alma

Yeni `features/online-payments/` klasörünü ve testi silin; değişen 5 dosyadaki ekleri geri alın. Veri/migration etkisi yoktur.

## Sonraki faz

FAZ 09 — Mobil ödeme ekranları ve offline sınırlar. Onay bekleniyor.
