# Anketler

Yönetici yolu `/api/v1/properties/{propertyId}/surveys`, politika `CanManageHierarchy` (SUPER_ADMIN/SITE_ADMIN/MANAGER). JWT `sub` ve aktif yönetim alanı üyeliği (veya SUPER_ADMIN) zorunludur. Yazım aktif Property ister; pasif/silinmiş üst kayıt `BF_PARENT_INACTIVE`.

| İstek | Davranış |
| --- | --- |
| GET `?page=1&pageSize=10&search=&state=&category=&from=&to=` | Sayfalı liste; pageSize 1–100, search ≤200 (başlık/açıklama), `state` DRAFT/SCHEDULED/ACTIVE/EXPIRED/COMPLETED, `from≤to` (tarih aralığıyla kesişen anketler). `stateCounts` (beş durum) ve `categoryCounts` (yedi kategori) sabit sırayla döner; kendi filtresini yok sayar |
| GET `/overview` | KPI'lar: toplam, bu ay oluşturulan, taslak, devam eden (aktif + planlanmış), tamamlanan, süresi dolan, yanıt/hedef sayıları, kategori dağılımı, en yüksek katılımlı 3 anket, son tamamlanan anket |
| GET `/{id}` | Anket ve soruları (seçenekleriyle) |
| GET `/{id}/results` | `Cache-Control: no-store`. Seçenek sayıları, puan ortalaması ve 1–5 dağılımı, en fazla 100 metin yanıtı. Anonim ankette 3'ten az yanıt varken `hidden=true` ve soru verisi yok; anonim metinlerde daire numarası her zaman null |
| POST | Taslak oluşturur (`version` 0), 201 |
| PUT `/{id}` | Yalnız taslak düzenlenir; sorular bütünüyle değiştirilir. Doğru `version` yoksa 409 `BF_RECORD_CHANGED` |
| POST `/{id}/copy` | Soruları dahil yeni taslak; süre korunur, başlangıç bugünden önce olamaz |
| PUT `/{id}/publish` | En az bir soru ve bitiş ≥ bugün; başlangıç ileri tarihliyse durum SCHEDULED |
| PUT `/{id}/schedule` | Yayındaki anketin bitiş tarihini uzatır (`endDate ≥ bugün`, `≥ başlangıç`) |
| PUT `/{id}/close` | Anketi kapatır; hedef kitle sayısı o anki değerle `eligible_count` alanına sabitlenir |
| DELETE `/{id}?version=` | Yalnız taslak, soft-delete, 204. Yayınlanmış anket ve yanıt geçmişi silinemez |

Gövde: `title` 1–200, `description` ≤2000, `category` DUES/SERVICE/SECURITY/SOCIAL/PARKING/ENVIRONMENT/GENERAL, `startDate`/`endDate` 2000–2100 ve `endDate ≥ startDate`, `audience` ALL_RESIDENTS/OWNERS/TENANTS, `responseMode` PER_UNIT (daire başına bir yanıt) / PER_ACCOUNT (hesap başına bir yanıt), `isAnonymous`, `questions` ≤30: `text` 1–500, `type` SINGLE_CHOICE/MULTIPLE_CHOICE/RATING/TEXT, `isRequired`, seçimli türlerde 2–10 benzersiz seçenek (≤200, büyük/küçük harf duyarsız). Hata kodları: `BF_SURVEY_INVALID`, `BF_SURVEY_QUESTION_INVALID` (400), `BF_SURVEY_STATE` (409).

Durum türetimi (Europe/Istanbul günü): DRAFT; PUBLISHED + başlangıç > bugün → SCHEDULED; PUBLISHED + bitiş < bugün → EXPIRED; diğer PUBLISHED → ACTIVE; CLOSED → COMPLETED.

## Sakin (mobil) yanıtı

`/api/v1/mobile/surveys`, kimlik doğrulama + mobil oturum bağlamı (`propertyId`, `unitId`, `role` OWNER/TENANT/RESIDENT). Bağlam için Property üyeliği, `Person.LinkedUserId` ve aktif daire oturumu gerekir; başka daire/rol 403.

| İstek | Davranış |
| --- | --- |
| GET `?propertyId&unitId&role` | Bağlama uygun, açık (ACTIVE) anketler ve bu bağlamın yanıtlayıp yanıtlamadığı |
| GET `/{id}?propertyId&unitId&role` | Soru formu; hedef kitle dışı 403 |
| POST `/{id}/responses` | `{propertyId, unitId, role, answers:[{questionId, optionIds?, rating?, text?}]}`; zorunlu sorular, tür/seçenek uyumu, tek seçimde tek seçenek, puan 1–5, metin ≤1000 doğrulanır (`BF_SURVEY_ANSWER_INVALID`). Açık değilse 409 `BF_SURVEY_NOT_OPEN`, ikinci yanıt 409 `BF_SURVEY_ALREADY_ANSWERED` |

Tek yanıt kuralı `survey_participations (survey_id, ballot_key)` benzersiz indeksi ve anket satırı üzerinde `FOR UPDATE` kilidiyle korunur. Ballot anahtarı PER_UNIT için `UNIT:{unitId}`, PER_ACCOUNT için `ACCOUNT:` + kullanıcı kimliğinin SHA-256 özetidir.

## KVKK / anonimlik

- Katılım satırı (kim yanıtladı) yalnız ikinci yanıtı engellemek için tutulur ve sonuç API'sinde hiç dönmez.
- Anonim ankette `survey_responses`/`survey_answers` satırları katılım veya daire bağlantısı taşımaz (DB check constraint). Audit alanları `anonymous` ve gün başına yuvarlanmış UTC zamanla yazılır; böylece zaman damgasıyla eşleştirme yapılamaz.
- Anonim sonuçlar 3 yanıta ulaşmadan gösterilmez; CSV dışa aktarım aynı sonuç API'sini kullanır ve formül enjeksiyonuna karşı `=`, `+`, `-`, `@`, sekme ile başlayan hücreleri `'` ile etkisizleştirir.
- Anonim olmayan ankette metin yanıtlarının yanında yalnız daire numarası görünür; kişi adı gösterilmez.

Tablolar: `surveys`, `survey_questions`, `survey_options`, `survey_participations`, `survey_responses`, `survey_answers` (BaseEntity kolonları, bileşik Property FK'ları, check constraint'ler). Anket geçmişi olan Property silinemez. Migration `20261021195918_Surveys` yalnız ekleme yapar; `Down` anket varsa durur.
