# Satılık / kiralık ilanlar — WEBSITE FAZ 06

Güncelleme: 19.10.2026. Uygulama mevcuttur; **gerçek ilan yayını kapalıdır**. Varsayılan `IListingAuthorityVerifier` yalnız `null` döndürür. Üretim EİDS entegrasyonu, sağlayıcı erişimi ve hukuki kabul bu teslimde yoktur. Yönetici onayı veya özel belge hiçbir zaman EİDS sonucu olarak gösterilmez. Bu nedenle T21.6 ve faz kabulü tamamlandı işaretlenmedi; FAZ07 başlamadı.

## Yönetim

Web Sitesi Yönetimi → **Satılık / Kiralık İlanlar**. Mevcut SUPER_ADMIN/SITE_ADMIN/MANAGER düzenler; yalnız SUPER_ADMIN/SITE_ADMIN inceleme onayı/red/kaldırma/yayın uçlarını kullanır. Aktif Property üyeliği, silinmemiş/etkin üst kayıt ve etkin canonical session modunda güncel yönetim görevi yeniden doğrulanır. Yeni Keycloak rolü yoktur. OWNER/RESIDENT/ACCOUNTANT'a yönetim yetkisi açılmaz.

Kök `/api/v1/properties/{propertyId}/listings`:

| Yol | İşlem |
| --- | --- |
| GET kök / GET `/{id}` | En fazla500 mevcut ilan / tek ilan; yönetim listesi istemcide sayfalanır |
| GET `/unit-options` | Aynı alandaki en fazla500 aktif bölüm; seçim isteğe bağlı |
| POST kök / PUT `/{id}` | UUID+normalize fingerprint oluşturma replay; içerik güncellemede version |
| DELETE `/{id}?version=` | Yalnız DRAFT; soft-delete, olay geçmişi korunur |
| PUT `/{id}/photos` | `{version,mediaIds}`; aynı site, aktif medya, benzersiz en fazla20; dizi sırası, ilk kapak |
| POST `/{id}/owner-consent` | multipart file/version/kind/agentLicenseNo; özel PDF/JPEG/PNG max10MiB |
| GET `/{id}/owner-consent` | JWT+üyelik, attachment/no-store; boyut ve SHA kontrolü |
| POST `/{id}/submit` | Belge ve fotoğraf zorunlu; DRAFT/WITHDRAWN/EXPIRED → PENDING_APPROVAL |
| POST `/{id}/approve` | version + complianceConfirmed:true + reason (inceleme notu) → APPROVED |
| POST `/{id}/reject` | version+reason → DRAFT |
| POST `/{id}/publish` | version+publishUntil, max90gün; sunucu sağlayıcısı geçerli EİDS yetkisi dönmezse **409 BF_LISTING_VERIFICATION_REQUIRED** |
| POST `/{id}/withdraw` | version+reason → WITHDRAWN; fotoğraf public erişimi hemen kesilir |
| GET `/{id}/events` | Değişmez işlem geçmişi; ad/iletişim public DTO'ya alınmaz |

Fiyat TRY, pozitif en fazla2ondalık; RENT aylık. Brüt/net pozitif2ondalık, net≤brüt. Oda `1+1`–`99+9`. Kapalı özellik kodları Application/Listings.cs içindedir. Başlık≤120/açıklama≤4000; HTML çalıştırılmaz. Unit bağlantısı **UnitOccupancy oluşturmaz veya değiştirmez**; kat/blok etiketleri açıkça yazılır, özel Unit numarası/adres/kişi otomatik aktarılmaz.

Malik belgesi türleri WRITTEN_STATEMENT/AUTHORIZED_AGENT; aracı için belge numarası zorunlu. Tasarımdaki EIDS_VERIFIED beyan seçeneği uygulanmadı: doğrulanmış sunucu yanıtı yerine kullanılamaz. Onaylanmış/yayınlı içerik, fotoğraf veya belge değişikliği onayı/verificationReference/yayın süresini kaldırıp yeniden incelemeye döner. Taslakta düzenleme taslak kalır. Aynı yönetici başvuru ve inceleme yapabilir; açık onay+not ve ayrı audit olayları zorunludur. İkinci onaylayan ayarı henüz yoktur.

## Anonim uçlar

`GET /api/public/v1/sites/{siteKey}/listings?kind=SALE|RENT&page=1` → en fazla12 kart. `GET .../listings/{id}` → ayrı public DTO. Yalnız aktif yayınlı site/Property ve PUBLISHED, süresi geçmemiş ilan. Scope dışında/askıda/süresi dolmuş kayıt genel404. Worker çalışmasa da süre kontrolü sorguda uygulanır; Worker ayrıca EXPIRE olayı yazar. Yetki belgesi, objectKey, Unit/Property/Person kimliği, audit/version ve kişisel iletişim public yanıtta yoktur.

Public medya mevcut `/api/public/v1/sites/{siteKey}/media/{id}/{sha}.{jpg|png}` yoludur. Aktif CMS yayını veya aktif yayınlı ilan referansı zorunlu; fotoğraflar same-site composite FK ile bağlıdır. İlan kaldırılınca sırf o ilanın fotoğrafı için verilen erişim kesilir. CMS'de de kullanılıyorsa o bağımsız yayın referansı geçerlidir. Public website `/[siteKey]/ilanlar`, `/[siteKey]/ilanlar/[id]`; no-store, redirect reddi, SHA doğrulayan mevcut medya proxy'si. `/` kökü demo olarak kalır; gerçek ilanlar burada gösterilmez.

## Başvurular ve saklama — T21.10

`GET /api/public/v1/sites/{siteKey}/inquiries/challenge` → token, aktif yayın SHA'sı olarak privacyNoticeVersion, yayınlı PRIVACY yolu. Token site+IP HMAC+aktif aydınlatma sürümü+sunucu saatiyle bağlı, 3saniye–30dakika. Gizli anahtar yoksa409 BF_INQUIRY_UNAVAILABLE, form kapalıdır.

`POST .../inquiries` → listingId?, fullName≤100, phone≤40 veya email≤200, message≤2000, privacyNoticeVersion, challenge, website(honeypot boş); **202 gövdesiz**. Gövde16KiB sınırı; eski metin/yanlış ilan/kısa süre/geçersiz token400. Honeypot gönderimi kayıt oluşturmadan202. API raw IP saklamaz; en az32bayt sunucu HMAC anahtarıyla günlük hash. Önceki gün hash'i saat sınırında kontrol edilir. PostgreSQL advisory+Property kilidi çoklu API instance'ında IP5/saat ve site100/gün bütçesini korur; limit429+Retry-After3600. İstemcinin elapsedMs beyanına güvenilmez. Token/iletişim/başvuru gövdesi loglanmaz.

`GET /api/v1/properties/{propertyId}/website/inquiries`, `PUT .../{id}` `{version,status:NEW|HANDLED|SPAM}` yalnız yönetim kapsamındadır. En fazla500 güncel başvuru gösterilir; süre dolmuş başvuru Worker gecikse de okunmaz. Worker başlangıçta ve saatte bir çalışır;180gün dolunca ad/telefon/e-posta/mesaj/IP hash'ini anonimleştirir.500'lük tüm partiler işlenir. Yedeklerin ayrı saklama/silme politikası işletim sorumluluğudur. Pazarlama/otomatik SMS/e-posta gönderimi yoktur. Aydınlatma metninin işletmeye uygunluğu ayrıca onaylanmalıdır.

Website aynı origin `/inquiries/{siteKey}` proxy'sini kullanır; POST Origin eşleşmesi, JSON16KiB sınırı, sabit API origin, timeout, redirect reddi; auth/cookie aktarılmaz. **Proxy ziyaretçinin ham IP'sini veya güvenilmeyen X-Forwarded-For'u API'ye iletmez. Bu nedenle proxy arkasında IP5/saat kotası tüm ziyaretçilerce paylaşılabilir.** Güvenilir istemci IP mimarisi/domain dağıtımı henüz kabul edilmedi; kota gevşetilmedi. Bu nedenle T21.10 tam işletim kabulü açık tutuldu.

## Kurulum ve yayın engeli

Yeni paket yok. API/Worker sunucu yapılandırmasında `Website:InquiryHmacKey` (ortam karşılığı `Website__InquiryHmacKey`) en az32rastgele baytın Base64 değeridir; secret store kullanın. NEXT_PUBLIC değişkenine/Git'e/loga koymayın. Anahtar döndürülürse eski challenge geçersizleşir. Mevcut .env/hesap/parola değiştirilmedi; testler sadece fixture anahtarını kullanır.

Yeni migration `20261018201251_PropertyListings`: property_listings, listing_photos, listing_events, website_inquiries;8ortak audit alanı NOT NULL, same-property/site FK, event UPDATE/DELETE trigger reddi. Dolu veride Down reddeder; geri alma veri silerek yapılmaz. Migration **yalnız geçici test DB'sinde uygulandı**. Yerel gerçek API'yi yeni kod/migration ile açmak için mevcut [migration prosedürünü](../MIGRATIONS.md) ve `bash scripts/dev.sh api` akışını kullanın; yönetim `bash scripts/dev.sh web`, website `npm run dev --prefix website`. Canlı servisler bu teslimde yeniden başlatılmadı.

EİDS yayını için sonraki somut gereksinim: Bakanlık/sağlayıcı sözleşmesi ve test erişimiyle gerçek `IListingAuthorityVerifier` uyarlaması, kimlik/yetki/süre/ilan bağının sunucuda doğrulanması ve negatif sağlayıcı testleri. Bir ortam bayrağıyla veya yönetici checkbox'ıyla sağlayıcı taklit edilmez. Hukuki dayanak için [Ticaret Bakanlığı EİDS](https://eids.ticaret.gov.tr/) ve [elektronik ilan açıklaması](https://ticaret.gov.tr/haberler/ticaret-bakanligi-tasinmaz-ve-tasit-ticaretinde-uygulanmakta-olan-elektronik-ilan-dogrulama-sistemi-eids-kapsaminda-kimlik-ve-yetki-dogrulamasi-zorunlulugunu-instagram-facebook-ve-whatsapp-da-dahil-olmak-uzere-elektronik-ortamda-verilen-tum-tasinmaz-ve-tasi) incelendi; bu belge hukuki kabul yerine geçmez.

Kabul kanıtları [TESTING.md](../TESTING.md). Canlı Keycloak, gerçek EİDS, hukukçu/gerçek kullanıcı kabulü, Lighthouse ve tam backend suite bu teslimin başarılı test iddiası değildir. Public duyuru tablosu, domain/SEO/caching ve giriş entegrasyonu bu ilan değişikliğinde uygulanmadı.
