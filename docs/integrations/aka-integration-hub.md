# AkaIntegrationHub entegrasyonu (BAKIM — 02.11.2026)

E-posta (SMTP) hesap tanımı, bağlantı testi, test e-postası ve gönderim geçmişi BuildingFlow'da kodlanmaz;
**Yönetim > Entegrasyonlar** (`/integrations`) menüsü AkaIntegrationHub yönetim ekranını açar.
Kimlik ortak Keycloak realm'i `buildingflow` üzerinden SSO ile sağlanır; ikinci giriş istenmez.

## Akış

1. Yönetici (`SUPER_ADMIN`, `SITE_ADMIN`, `MANAGER`) menüden **Entegrasyonlar**'ı açar.
2. Kart bağlantısı Hub'ı aynı sekmede açar:
   `<HUB>/admin/email/providers?idp=buildingflow&host=BuildingFlow&locale=tr|en&theme=light|dark&return=<BuildingFlow sayfası>`.
   URL'de token, kod veya kişisel veri taşınmaz; `return` sorgu/fragment içermez.
3. Hub, `aih-admin-web` public client'ı ile (Authorization Code + S256 PKCE) aynı realm'den oturum açar.
   Keycloak oturumu zaten açık olduğu için kullanıcıya giriş ekranı gösterilmez.
4. Hub API, token'ı kendi kayıtlı issuer listesinden doğrular; tenant ve rol token claim'inden değil kayıttan türetilir.
5. Hub'daki "← BuildingFlow'a dön" bağlantısı `return` adresine döner (Hub tarafında origin allowlist'i).

## Yapılandırma

| Yer | Anahtar | Yerel değer |
|---|---|---|
| `frontend/buildingflow-web/.env` | `NEXT_PUBLIC_AKA_INTEGRATION_HUB_URL` | `http://localhost:28030` |
| Keycloak realm `buildingflow` | client `aih-admin-web` | `deploy/keycloak/realm-buildingflow.json` içinde |
| Hub `apps/admin-web/.env` | `NEXT_PUBLIC_AIH_IDPS`, `NEXT_PUBLIC_AIH_HOST_RETURN_ORIGINS` | `buildingflow` IdP, `http://localhost:3003` |

Değer tanımlı değilse ekran "adres tanımlı değil" uyarısı gösterir; bağlantı üretilmez. Değişiklikten sonra web yeniden derlenir.
Mevcut çalışan Keycloak'a realm dosyası otomatik yeniden yüklenmez; client'ı admin konsolundan aynı ayarlarla ekleyin
(public, standard flow, PKCE S256, redirect `http://localhost:28030/*`, web origin `http://localhost:28030`).

## Hub'da tenant kaydı (bir kez)

```bash
uv run aih-api register-tenant --slug buildingflow --name "BuildingFlow" \
  --issuer http://localhost:8080/realms/buildingflow --client-id aih-admin-web \
  --tenant-admin-roles SUPER_ADMIN,SITE_ADMIN --viewer-roles MANAGER
```

Rol eşlemesi: `SUPER_ADMIN`, `SITE_ADMIN` → Hub `TenantAdmin` (tanım/test/gönderim); `MANAGER` → Hub `Viewer` (salt okuma, host/kullanıcı maskeli).
`ACCOUNTANT`, `SECURITY` ve sakin rolleri menüyü görmez; Hub da bu rolleri kabul etmez.

## Sınırlar

- Hub ekranı iframe ile gömülmez; ayrı uygulama olarak açılır.
- BuildingFlow backend'i Hub'a çağrı yapmaz; SMTP gizli bilgisi BuildingFlow'a hiç gelmez.
- Yeni paket eklenmedi (ikon `lucide-react` `Plug`, mevcut set).
