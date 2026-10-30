# TEDARİKÇİ FAZ09 — Mobil ve offline-first

30.10.2026; dal `buildingflow_tedarikci`; [GitHub #99](https://github.com/AkaBayraK/BuildingFlow/issues/99). Plan FAZ09, kayıt10/13, resmi3/3=%100,00. Tedarikçi akışı10/13=%76,92. Sonraki FAZ10'dur. Bu iş TEDARİKÇİ faz sayacına aittir; MOBİL 15 faz sayacına eklenmez.

**Yönetici mobil liste, detay, iş emri, fotoğraf, not, senkronizasyon kuyruğu.** Yönetici ana sayfasında "Tedarikçi işleri" kısayolu ve gizli sekme rotası. Seçili yönetim alanındaki tüm tedarikçilerin iş emirleri (durum ikonu, tedarikçi, hizmet, SLA/gecikme, fotoğraf sayısı); kart açılınca açıklama, izinli sonraki durumlar, saha notu ve kamera/galeriden 256 KB JPEG fotoğraf. Backend'e mülk bazlı liste ucu ve immutable `NOTE` revizyonlu saha notu ucu eklendi; web iş emri geçmişi notu gösterir.

**SQLite kapsamı, şifreleme, kullanıcı değişiminde temizleme, conflict handling.** Liste kopyası ve kuyruk mevcut hesap başına SQLCipher şifreli depoda (anahtar SecureStore'da), fotoğraf AES ile şifreli özel dosyada; yalnız gerekli alanlar saklanır, token/kişisel iletişim verisi yok. Kopya 1 saat geçerli. Hesap değişince depo anahtarıyla birlikte silinir; yetkisi kalkan bağlamın işlemleri, fotoğrafları ve kopyası silinir ve gönderilmez. İş emri başına tek aktif işlem; 409 çakışmada sunucu kaydı korunur, yönetici "Vazgeç" ile kaldırır ve güncel listeden yeniden dener.

**Offline senaryolar ve tekrar bağlantı testleri.** `tests/supplier-work.test.mjs` (gerçek node:sqlite ile şema 10): yetkili bağlam ve rol, yol beyaz listesi, DTO kapsam/terminal gecikme reddi, komut doğrulama; çevrimdışı kuyruk → bekleme → bağlantı gelince aynı işlem kimliğiyle tek gönderim; yarıda kalan gönderimin aynı kimlikle tekrarı; 409 çakışma ve dosya temizleyen vazgeç; 401 durdurma, 5xx geri çekilme → başarısız → elle yeniden deneme → 403 engel; yetki kaybında temizlik ve başka hesap izolasyonu; iptal edilen ekranın gönderim yapmaması. Backend: not replay/çakışma/kapalı iş emri, mülk listesi filtre/kapsam/rol/üyelik testleri.

| Bu işte çalıştırılan doğrulama | Sonuç |
| --- | --- |
| Tam backend regresyonu | **Integration931/933 + Unit472/472**, 2 opt-in canlı OIDC atlandı |
| Mobil `npm run check` (typecheck+lint+test) | **170/170, GEÇTİ** |
| Mobil `npm run build` (iOS/Android/web export) | **GEÇTİ** |
| Web test + typecheck + build | **340/340, GEÇTİ, GEÇTİ** |
| Kapsam kapısı / lisans | **GEÇTİ** (API 460, mobil ekran 10) / **329 PASS**, yeni paket yok |

Loglar `/tmp/bf-sup09-{a,mobile,mobilebuild,full,webbuild}.log`. Gerçek cihaz veya simülatörde uçtan uca deneme, SQLCipher'ın native build'de çalışması ve gerçek kamera akışı bu turda doğrulanmadı; testler kuyruk/veritabanı mantığını node:sqlite ile, ağ katmanını sahte uçlarla sınar. Yalnız yeni yetki modelindeki yönetim bağlamları desteklenir (tenant bilgisi gerekir); eski bağlam modelinde ekran kilit mesajı gösterir. Migration yerel veritabanına uygulanmadı.

Giriş token: BİLGİ MEVCUT DEĞİL. Çıkış token: BİLGİ MEVCUT DEĞİL. Toplam token: BİLGİ MEVCUT DEĞİL. Kalan context: BİLGİ MEVCUT DEĞİL.
