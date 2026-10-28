# [QA FAZ 07 | KAYIT 08/14] Mobil ve offline-first otomasyonu — GitHub #103

```text
[BUILDINGFLOW]
[FAZ QA 07 | KAYIT 08/14] Mobil ve offline-first
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR (#103 açık)
[İLERLEME] FAZ 07 PHASES maddeleri 1/5 = %20,00 (cihaz otomasyonu BLOKE)
```

## Bu tur
- `npm run check` (typecheck + lint + **160/160 test**) iki kez ve `npm run build` (3 platform export) **PASS**.
- **QA mobil profili** (FAZ 01'den taşınan): `eas.json` içinde `qa` profili; `development`'tan türer, iOS simülatör build'i, `BUILDINGFLOW_QA_PROFILE=1`. Üretim profiline dokunulmadı. release testleri PASS.
- **Mobil ekran içi rol eşlemesi** (FAZ 02'den taşınan): `scripts/qa_inventory.py` `mobileRoleGates` üretir (statik: kaynakta geçen rol/ilişki adları):


| Modül/ekran | Roller |
|---|---|
| `auth` | ACCOUNTANT, MANAGER, OWNER, RESIDENT, SITE_ADMIN, SUPER_ADMIN, TENANT |
| `common-areas` | MANAGER, OWNER, RESIDENT, SITE_ADMIN, SUPER_ADMIN, TENANT |
| `components` | ACCOUNTANT, TENANT |
| `finance` | ACCOUNTANT, MANAGER, OWNER, SITE_ADMIN, SUPER_ADMIN, TENANT |
| `manager` | MANAGER, OWNER, RESIDENT, SITE_ADMIN, SUPER_ADMIN, TENANT |
| `offline` | MANAGER |
| `owner` | OWNER, TENANT |
| `parking` | MANAGER, OWNER, RESIDENT, SITE_ADMIN, SUPER_ADMIN, TENANT |
| `person-authorization` | ACCOUNTANT, MANAGER, OWNER, RESIDENT, SITE_ADMIN, SUPER_ADMIN, TENANT |
| `screen:index` | OWNER, TENANT |
| `screen:profile` | SITE_ADMIN, SUPER_ADMIN |
| `screen:requests` | ACCOUNTANT |
| `sync` | ACCOUNTANT |

- **iOS simülatör denemesi:** mevcut dev build (`com.buildingflow.mobile.dev`, 13.10.2026) iPhone 17 Pro simülatörüne kuruldu ve dev client açıldı. Uygulama JS paketini yükleyemedi: çalışan Metro (8081, `--lan`) 170 sn'de yanıt vermedi; kendi başlattığım Metro (8082, `--localhost`) paketi host'a 7,1 MB/200 ile verdi ama simülatördeki dev client "Could not connect to development server" gösterdi. Kanıt: `artifacts/qa/qa-mobile-ios/01-launch.png`, `02-devserver-error.png`. Kendi Metro'm ve simülatör kapatıldı; sizin 8081 Metro'nuza dokunulmadı. Olası neden: dev build güncel değil (yeniden `npx expo run:ios` gerekir, ~10+ dk).

## Offline/sync ↔ TEST_MATRIX (mevcut 160 testin kapsamı)
| Satır | Kanıt (test adı özeti) |
|---|---|
| SQLite migration / şema | v1→v8 yükseltme, tekrar uygulama, gelecek şema reddi |
| Hesap/tenant izolasyonu | başka hesap cache okuyamaz/yazamaz; logout kuyruğu geçersizler |
| Queue/sync, kayıp yanıt | sunucu commit sonrası kayıp yanıt aynı operation ID ile tekrar, mükerrer yok |
| Çakışma | 409 açık çakışma, otomatik tekrar yok; yeni snapshot kazanır |
| Retry | 429 sınırlı bekleme, bütçe tükenince kimlik değişmez |
| Uygulama kapanması | yeniden başlatmada gönderilmekte olan işlem kurtarılır |
| Yetki kaybı | süresi dolmuş/düşürülmüş yetki gönderemez; iptal edilen bağlam temizlenir |
| Cache süresi | 24 saat sınırı, geri saat, bozuk payload kapalı başarısız |

## PHASES kontrol listesi
| Madde | Durum |
|---|---|
| iOS/Android kurulum, oturum, rol/daire değiştirme, ekran CRUD | **BLOKE** (iOS: dev build/Metro bağlantısı; Android: SDK yok) |
| SQLite offline, sync, çakışma, tekrar deneme | PASS (birim/sözleşme testleri, cihaz üzerinde değil) |
| Bağlantı kopması, kapanma, background/foreground | KISMİ (kayıp yanıt ve yeniden başlatma test; gerçek cihaz yaşam döngüsü yok) |
| Bildirim, fotoğraf/KVKK izinleri, ekran kanıtı | KISMİ (fotoğraf outbox rollback testi; izin diyalogları cihazda denenmedi) |
| Online ödeme kuralı | BLOKE (#89: istemci yok) |

Web ↔ mobil görünüm karşılaştırması (FAZ 05'ten taşınan) cihaz otomasyonu olmadan **BLOKE**.

## Sonraki için gereken
1. Güncel iOS dev build (`npx expo run:ios` veya `eas build --profile qa`), 2. Android SDK + emülatör, 3. cihaz UI otomasyonu için araç kararı (yeni paket yasağı: Maestro/Detox lisans onayı veya simctl + koordinat tabanlı kendi betiğimiz).

[TOKEN] BİLGİ MEVCUT DEĞİL.
