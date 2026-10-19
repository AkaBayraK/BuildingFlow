# Mobil duyuru ve kişisel bildirim kutusu

Kimlik doğrulama gerekir; API yalnız yetkili MobileSession property/unit/role bağlamını kullanır. Anonim401; kapsam/yayın/alıcı/üyelik reddi403; geçersiz sorgu400; tercih sürüm çakışması409.

- `GET /api/v1/mobile/notices?propertyId=<uuid>&role=<role>&kind=ANNOUNCEMENT|MESSAGE&page=1[&unitId=<uuid>][&itemId=<uuid>]`. Sayfa1–10000, sayfa boyutu50. userId/contextKey/asOf/propertyName/kind/page/itemId/total/unread/preference/pushAvailable/items döner. itemId total’ı detayla sınırlar; unread tüm yetkili kutunun sayacıdır. Item yalnız id/title/body/createdAt/readAt taşır. ANNOUNCEMENT yalnız yayınlanmış ve seçili role uygun audience; MESSAGE yalnız bu kullanıcıya gönderilen aktif üyelik kapsamı. PersonId/recipientUserId/iletişim/kimlik/banka ayrıntıları aktarılmaz.
- `POST /api/v1/mobile/notices/read`: `{propertyId,unitId,role,kind,id}`. İlk ReadAt korunur. Hesap+duyuru tekil kayıt ve advisory lock; mesajda mevcut PersonMessage.ReadAt. `{id,kind,readAt}` makbuzu aynı kimliği doğrular. Finansal işlem veya yeni mesaj gönderimi yapmaz.
- `POST /api/v1/mobile/notices/preference`: `{propertyId,unitId,role,highlightsEnabled,expectedVersion}`. `{highlightsEnabled,version}` döner. Hesap başına tek tercih; başlangıç true/0, yeni kayıt version1. Önceki version’ın aynı yeni değerle bir kez tekrarı mevcut makbuzu verir; diğer eski sürümler409. İçeriği gizlemez, okundu saymaz, push aboneliği değildir.

`pushAvailable` şu an false. Push token/sağlayıcı/arka plan teslimi yoktur; hassas içerik OS bildirimi olarak üretilmez. Tüm metinler yalnız doğrulanmış uygulama içi kutudadır. Mobil kopya mevcut şifreli cache içinde24saat TTL ve scope ayrımıyla saklanır. Kaynak/API/persistence kanıtı ve migration/rollback sınırı: `mobile/docs/mobile/PHASE13.md`.
