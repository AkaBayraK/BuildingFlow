# Mobil hesap bağlamı — Faz 04

13.10.2026. Kullanıcı mobil Faz04 için backend/Keycloak genişletmesini onayladı.
GET `/api/v1/mobile/session`, authenticated + zorunlu sub. Yanıt userId/username/contexts/hasMore; contexts propertyId/propertyName/unitId/unitNumber/role.
SUPER_ADMIN mevcut global aktif Property kapsamı; SITE_ADMIN/MANAGER/ACCOUNTANT aktif PropertyMembership kapsamı. OWNER/TENANT/RESIDENT yalnız linked Person+aktif üyelik+aktif kişi/daire/bina+bugün dahil geçerli Occupancy. Soft-delete filtreleri korunur. Rol bağlamı mevcut yönetici endpoint izinlerini genişletmez. Başka hesabın kişisi veya kimlik/iletişim alanları döndürülmez. Boş contexts geçerli; en fazla500 ve hasMore. Şema/migration yok.
Mobil public client buildingflow-mobile: S256, basic scope, buildingflow-api audience, password/implicit grant kapalı. Basic scope sub claim üretir; mevcut client update'te default-client-scopes ilişkisi ayrıca bağlanır. Development issuer allowlist yalnız Development'ta açık config; Production güvenliği korunur.
Üretici: Application/MobileSession, Infrastructure/MobileSessionStore, API/MobileSessionEndpoints. Tüketici: ayrı buildingflow-mobile/mobile/src/auth.
Kabul: izole PostgreSQL mobile+auth+release güvenliği45/45; mobil TypeScript/lint8/8; üç platform export; Doctor21/21. Gerçek geçici Keycloak fixture ile PKCE giriş/API session/profil/çıkış; native cihaz/SecureStore kabulü yapılmadı. Şimdilik mobil API development port5006, ana API5005 korunur. Runtime imzalama ve HTTPS üretim hazırlığı değildir.
Ayrıntı: [Mobil rapor](../../../buildingflow-mobile/docs/mobile/PHASE04.md).
