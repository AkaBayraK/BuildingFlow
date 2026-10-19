# Açık rıza ve KVKK API'si (İŞ16)

Kaynak: `backend/src/BuildingFlow.Api/ConsentEndpoints.cs`, `Application/Consents.cs`, `Application/ConsentMarkup.cs`, `Infrastructure/ConsentStore.cs`. Analiz ve kararlar: [PHASE_16_CONSENT_KVKK.md](../analysis/PHASE_16_CONSENT_KVKK.md).

## Kararlar

- Metinler sistem geneli ortaktır (Property bazlı değil).
- Rıza geçmişi silinmez: verdi → yeni sürümle yeniledi → geri aldı satırları tutulur. Geçmişi kullanıcı kendisi görmez; yalnız merkez (SUPER_ADMIN tümü, SITE_ADMIN/MANAGER kendi aktif Property üyelerinin kayıtları) görür.
- Yeni lisanslı paket yok: HTML yerine projeye ait kısıtlı biçim, PDF yerine yazdırılabilir sayfa + SHA-256 kanıt özeti.

## Veri modeli

| Tablo | Amaç | Önemli kısıtlar |
| --- | --- | --- |
| `consent_types` | Rıza türü kataloğu. Sistem türleri: `KVKK_INFORMATION` (INFORMATION), `PROFILE_PHOTO`, `REQUEST_PHOTO` (EXPLICIT_CONSENT, `feature` ile koda bağlı). Sabit id `6d1f5c2e-6a3b-4c8e-9a51-0c7e2b3f4a01/02/03`. | Kod `^[A-Z][A-Z0-9_]{1,39}$`, silinmiş satırlar dahil tekil; feature yalnız sistem türünde ve tekil; sıra 0–10000. |
| `consent_texts` | Değişmez, sürümlü metin. Her yayın yeni satırdır. | (tür, sürüm) ve (tür, operationId) tekil; `body_sha256` ve fingerprint 64 küçük hex; boş başlık/gövde yok. |
| `user_consents` | Kullanıcının onay geçmişi. | (owner, tür) için tek aktif satır (`revoked_at IS NULL AND is_deleted = false`); (text, type) composite FK başka türün metnini reddeder; geri alma nedeni WITHDRAWN/SUPERSEDED; `revoked_at >= approved_at`. |

Migration `20261016130236_Consents`: üç tablo + sistem türleri seed (`ON CONFLICT (code) DO NOTHING`). Down, metin veya onay satırı varsa veri kaybını reddeder.

Migration `20261016143930_DefaultConsentTexts`: üç sistem türüne özgün taslak v1 metin ekler (yalnız türün hiç metni yoksa; `created_by = system`, `requires_reconsent = true`). Metinler [default-consent-texts.md](default-consent-texts.md) dosyasındadır; merkezden yeni sürüm yayınlanarak değiştirilir. Karmalar migration'da sabittir, `DefaultConsentTextTests` yeniden hesaplar. Down, bu metinlere onay verilmişse reddeder, yoksa yalnız bu üç satırı siler.

## Durumlar

Satır durumu: `ACTIVE` (geçerli), `OUTDATED` (sonrasında `requiresReconsent = true` bir sürüm yayınlanmış, yeniden onay bekliyor), `WITHDRAWN` (kullanıcı geri aldı), `SUPERSEDED` (yeni sürüm onayıyla kapandı). Kişi özeti tür başına ek olarak `NONE` (hiç onay yok) döner; geri alınmışsa `WITHDRAWN`.

Yazım düzeltmesi için `requiresReconsent = false` yayınlanırsa mevcut onaylar `ACTIVE` kalır.

## Kanıt

`evidenceSha256 = SHA-256("consent-v1\n" + id, owner, typeCode, textId, textVersion, bodySha256, approverName, approvedAt (yyyy-MM-ddTHH:mm:ss.ffffffZ), channel, appVersion, operationId)` satırları `\n` ile birleştirilir. Belge uçları özeti her okumada yeniden hesaplar: `bodyIntact` (metin hâlâ onaylanan metin mi) ve `evidenceIntact` (onay satırı sonradan değişmiş mi).

## Uçlar

Grup `/api/v1/consents`, politika `CanManagePeople` (SUPER_ADMIN, SITE_ADMIN, MANAGER). Yazma uçları ayrıca `CanManageProperties` (SUPER_ADMIN). ACCOUNTANT ve RESIDENT 403, anonim 401.

| Uç | Açıklama |
| --- | --- |
| `GET /types` | Türler; `textCount` ve `currentText` özeti. |
| `POST /types` | `{code, nameTr, nameEn, kind, sortOrder}`. Kod çakışması 409 `BF_CONSENT_TYPE_CONFLICT`. |
| `PUT /types/{id}` | `{nameTr, nameEn, sortOrder, isEnabled, version}`. Sistem türü pasife alınamaz (409 `BF_CONSENT_SYSTEM_TYPE`); eski version 409. |
| `DELETE /types/{id}?version=` | Yalnız sistem dışı ve hiç metni olmayan tür (aksi 409 `BF_CONSENT_SYSTEM_TYPE` / `BF_CONSENT_TYPE_IN_USE`). |
| `GET /types/{id}/texts?page&pageSize` | Sürüm geçmişi, yeni sürüm önce. |
| `POST /types/{id}/texts` | `{operationId, title, body, requiresReconsent}`. Aynı operationId + aynı içerik aynı satırı döner; farklı içerik 409 `BF_CONSENT_OPERATION_CONFLICT`. Pasif tür 409 `BF_CONSENT_TYPE_DISABLED`. Eşzamanlı yayınlar tür satırı kilidiyle sıralanır. |
| `GET /texts/{id}` | Tek metin. |
| `GET /records?page&pageSize&typeId&state&propertyId&search&userId` | Onay kayıtları (yeni önce). `search` ad veya kullanıcı kimliği (ILIKE, kaçışlı, ≤100). `propertyId` görünür olmalı. |
| `GET /records/{id}` | Belge: kayıt, onaylanan metin, özetler ve bütünlük. Kapsam dışı 404. |
| `GET /users/{userId}` | Kişi özeti (etkin türler) ve en fazla 500 satırlık geçmiş. Kapsam dışı 404. |

## Metin biçimi

Paylaşılan kurallar ve test vektörleri: [consent-markup-vectors.json](consent-markup-vectors.json). C# (`ConsentMarkup`), web (`features/consents/markup.ts`) ve ileride mobil aynı vektörleri geçmek zorundadır.

`#`/`##`/`###` başlık, boş satır yeni paragraf, `- ` madde, `1. ` numaralı madde (başlangıç numarası korunur), `**kalın**`, `[metin](https://…)` veya `[metin](mailto:…)`. HTML desteklenmez ve düz metin olarak kalır. Hatalar: `EMPTY`, `TOO_LONG` (>50.000), `TOO_MANY_LINES` (>2.000), `CONTROL_CHAR` (kontrol/görünmez biçim karakteri, bozuk surrogate; ZWNJ/ZWJ serbest), `LINK` (https olmayan, kullanıcı bilgili veya hostsuz bağlantı). API `BF_CONSENT_TEXT_INVALID` veya `BF_CONSENT_LINK_INVALID` döner. Saklanan gövde normalize edilmiş biçimdir (`\r\n`→`\n`, sekme→boşluk).

## Hata kodları

`BF_CONSENT_INVALID`, `BF_CONSENT_TEXT_INVALID`, `BF_CONSENT_LINK_INVALID` (400); `BF_CONSENT_TYPE_CONFLICT`, `BF_CONSENT_SYSTEM_TYPE`, `BF_CONSENT_TYPE_IN_USE`, `BF_CONSENT_OPERATION_CONFLICT`, `BF_CONSENT_TYPE_DISABLED` (409). TR/EN çevirileri `messages/*.json` → `apiErrors`.

## Web

Yönetim → **Açık Rıza** (`/consents`): Rıza türleri, Metinler (sürüm geçmişi, yayın diyaloğu, canlı önizleme, "yeniden onay gerekir"), Onay kayıtları (filtre, kişi geçmişi). Belge sayfası `/consents/records/{id}` yazdırılabilir; tarayıcıdan "PDF olarak kaydet" ile çıktı alınır. Kullanıcı onayı ve geri alma uçları, fotoğraf zorunluluğu ve kişi/talep ekranlarındaki rıza bilgisi İŞ17'dedir: [mobile-consents.md](mobile-consents.md).
