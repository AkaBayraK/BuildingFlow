# Mobil rıza API'si ve fotoğraf rızası zorunluluğu (İŞ17)

Kaynak: `backend/src/BuildingFlow.Api/MobileConsentEndpoints.cs`, `Application/MobileConsents.cs`, `Infrastructure/MobileConsentStore.cs`, `Infrastructure/ConsentGate.cs`. Merkez tarafı: [consents.md](consents.md). Analiz: [PHASE_16_CONSENT_KVKK.md](../analysis/PHASE_16_CONSENT_KVKK.md).

## Kararlar

- Hesap sahibi kendi rızasını yalnız mobilden verir ve geri çeker. Merkez kullanıcı adına rıza veremez ve fotoğraf yükleyemez (merkezde bunun için uç yok).
- Mobil uç **geçmiş döndürmez**: yalnız tür başına güncel durum ve onaylanacak güncel metin. Onay, yenileme ve geri çekme satırları silinmez; geçmişi yalnız merkez görür (`/api/v1/consents/...`).
- Yeni tablo veya migration yok. Tekrar güvenliği mevcut `mobile_operation_receipts` ile sağlanır, rıza satırları İŞ16'daki `user_consents` tablosundadır.

## Uçlar

Grup `/api/v1/mobile/consents`, kimliği doğrulanmış her hesap (mobil profil ile aynı kural: kişi bağlantısı olmayan hesaplar da kullanabilir). Anonim 401.

| Uç | Açıklama |
| --- | --- |
| `GET /` | `{userId, items:[{status, currentText}]}`. Yalnız etkin türler, sıra `sortOrder` + kod. `currentText` en son yayınlanan metin (`id, textVersion, title, body, bodySha256, requiresReconsent, publishedAt`); hiç metin yoksa `null`. |
| `POST /commands` | `{operationId, action, typeCode, textId?, bodySha256?, appVersion?}`. JSON, en çok 16 KiB (aşan 413, JSON olmayan 415). Yanıt `{operationId, status, replayed}`. |

`status`: `typeCode, nameTr, nameEn, kind, feature, sortOrder, state, approvedVersion, approvedAt, currentTextId, currentVersion`. `state` değerleri: `NONE` (hiç onay yok), `ACTIVE`, `OUTDATED` (sonrasında `requiresReconsent = true` bir sürüm yayınlandı), `WITHDRAWN` (en son geri çekildi, aktif onay yok).

### APPROVE

- `textId` ve `bodySha256` (64 küçük hex) zorunlu. Yalnız türün **en son** metni onaylanabilir ve gövde karması eşleşmelidir; aksi 409 `BF_CONSENT_TEXT_OUTDATED`. İstemci bu durumda güncel metni yeniden göstermelidir.
- Aktif onay aynı metinse yeni satır açılmaz (durum döner). Eski metne aitse eski satır `SUPERSEDED` olur, yeni satır açılır; ikisi tek transaction'dadır.
- Yeni satır: kanal `MOBILE`, onaylayan adı mobil profildeki ad soyad (yoksa kullanıcı adı), uygulama sürümü, işlem UUID'si ve İŞ16 kanıt özeti (`evidenceSha256`).
- Pasif tür 409 `BF_CONSENT_TYPE_DISABLED`; bilinmeyen tür kodu 400 `BF_CONSENT_INVALID`.

### WITHDRAW

- `textId`/`bodySha256` gönderilmez (gönderilirse 400).
- `INFORMATION` türü (aydınlatma okundu bilgisi) geri çekilemez: 409 `BF_CONSENT_NOT_WITHDRAWABLE`.
- Aktif onay yoksa işlem değişiklik yapmadan başarılı döner.
- `PROFILE_PHOTO`: aynı transaction'da profil fotoğraf işaretçisi boşaltılır, fotoğraf `RETIRED` olur, profil sürümü artar (eski sürümle bekleyen profil komutu 409 alır). `ProfilePhotoMaintenance` nesneyi depodan siler ve satırı `PURGED` yapar.
- `REQUEST_PHOTO`: talep fotoğraflarına dokunulmaz. Geri çekme anına kadar CURRENT olmuş her talep fotoğrafı sahibinin mobil sorgularından gizlenir: `GET /mobile/sync/requests` `photoId`/`photoSha256` için `null`, sahibin `GET /mobile/sync/requests/{id}/photo` isteği 404. Merkez (talep yönetimi ve mobil yönetici görünümü) fotoğrafı görmeye devam eder. Kişi yeniden onay verip yeni fotoğraf yüklerse yeni fotoğraf görünür; eskiler gizli kalır.

### Tekrar ve eşzamanlılık

- Aynı `operationId` + aynı içerik ilk sonucu `replayed = true` ile döner; farklı içerik 409 `BF_CONSENT_OPERATION_CONFLICT`. Parmak izi `consent-v1:` önekli JSON'un SHA-256'sıdır.
- Kilit sırası: işlem UUID kilidi → (profil rızası geri çekmede) `profile:{user}` → `consent:{user}`. Profil ve talep fotoğrafı yazımları da `consent:{user}` kilidini en son alır; geri çekme ile yükleme iç içe geçemez.

## Fotoğraf zorunluluğu (T17.2, T17.3)

| Akış | Kural |
| --- | --- |
| `POST /api/v1/mobile/profile/commands` `photoAction=REPLACE` | `PROFILE_PHOTO` rızası **ACTIVE** olmalı, aksi 403 `BF_CONSENT_REQUIRED`. |
| `POST /api/v1/mobile/sync/commands` `PhotoAction=REPLACE` | `REQUEST_PHOTO` rızası **ACTIVE** olmalı, aksi 403 `BF_CONSENT_REQUIRED`. |
| `KEEP`, `REMOVE`, fotoğrafsız talep | Rıza istemez; kişi fotoğrafını her zaman kaldırabilir, talebini fotoğrafsız kaydedebilir. |

- Kontrol iki kez yapılır: depolamaya yazmadan önce (rızasız istek STAGED satır ve nesne bırakmaz) ve transaction içinde kilitle. Daha önce tamamlanmış bir işlemin tekrarı (aynı UUID) kontrol yapılmadan önceki sonucu döner.
- Rıza yoksa, eski sürümse (`OUTDATED`) veya geri çekildiyse aynı 403 döner. Metin hiç yayınlanmamışsa kimse fotoğraf yükleyemez; yayın merkezden SUPER_ADMIN tarafından yapılır.

## Merkezde görünürlük (T17.5)

- `GET /api/v1/properties/{propertyId}/requests/{id}` yanıtına `photoConsent` eklendi (fotoğrafsız talepte `null`): `{state: GRANTED | WITHDRAWN | NONE, recordId, textVersion, approvedAt, withdrawnAt}`. Fotoğrafın CURRENT olduğu anda geçerli rıza satırı ve sonrasındaki ilk geri çekme gösterilir. İŞ17 öncesi yüklenen fotoğraflar `NONE` döner ve sahibine görünmeye devam eder.
- Web: talep detayında fotoğrafın altında "Fotoğraf rızası v3 · tarih" veya "Rıza … tarihinde geri çekildi; kişi bu fotoğrafı artık görmüyor" ve rıza belgesi bağlantısı. Kişi sayfasında **Açık rıza** bölümü: bağlı giriş hesabının tür başına durumu ve İŞ16 geçmiş penceresi. Bağlı hesap yoksa bunu söyler; kapsam dışı hesapta (404) "görme yetkiniz yok" yazar.

## Hata kodları

`BF_CONSENT_REQUIRED` (403), `BF_CONSENT_TEXT_OUTDATED` (409), `BF_CONSENT_NOT_WITHDRAWABLE` (409); İŞ16 kodları `BF_CONSENT_INVALID`, `BF_CONSENT_TYPE_DISABLED`, `BF_CONSENT_OPERATION_CONFLICT`. Kodlu 403 için `CodedForbiddenException` eklendi; `ExceptionHandler` bu hatanın kod ve mesajını ProblemDetails'e yazar. TR/EN çevirileri `messages/*.json → apiErrors`.

## Mobil istemci (İŞ18)

Uygulandı (16.10.2026), yeni paket yok. Kod `mobile/buildingflow-mobile/src/consents/`, ayrıntı [IS18_CONSENT.md](../../mobile/docs/mobile/IS18_CONSENT.md).

- **Okuma:** `GET /consents` yanıtı `parseList` ile doğrulanır (`userId` oturumla eşleşmeli, metin–durum eşleşmesi, tür tekilliği, en çok 50 tür). Her metin gövdesinin SHA-256'sı `bodySha256` ile karşılaştırılmadan önbelleğe yazılmaz ve onaya sunulmaz. Liste hesaba bağlı şifreli SQLite'ta (`consent_cache`) tutulur; çevrimdışı ekranlar buradan okunur.
- **Gösterim:** metin WebView olmadan, İŞ16 ayrıştırıcısının mobil kopyasıyla RN `Text` olarak çizilir; ortak vektörler mobil testte birebir. Ayrıştırılamayan metin onaya sunulmaz.
- **Komut:** onay/geri çekme önce `consent_jobs` kuyruğuna yazılır, en eskiden başlayarak tek tek gönderilir. Gövde yalnız gerekli alanları taşır (geri çekmede `textId`/`bodySha256` yok). Makbuz `parseResult` ile komuta bağlanır. Sıralama: rıza kuyruğu talep eşitlemesinden ve profil eşitlemesinden önce boşaltılır.
- **Hatalar:** 409 `BF_CONSENT_TEXT_OUTDATED` → iş `conflict`, liste yenilenir, kişiye yeni sürüm sorulur. 401 → oturum yenileme akışı. 429/5xx/ağ → üstel bekleme (2 sn × 2^deneme, en çok 120 sn, `Retry-After` alt sınır), 6 deneme.
- **Bekletilen fotoğraf:** talep veya profil komutu 403 `BF_CONSENT_REQUIRED` alırsa silinmez; yerel hata kodu 4031 ile bekletilir, ilgili rıza onayı gönderilince otomatik yeniden denenir. Kişi bekleyen işi iptal edebilir.
- **Geri çekme:** başarıdan sonra bu cihazdaki ilgili yerel fotoğraflar silinir. Kişi rıza geçmişini görmez; yalnız güncel durum gösterilir.
