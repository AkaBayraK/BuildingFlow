> 2026-10-08: Bu belge 02 fazının tarihsel sözleşmesidir. Güncel ortak model ve kanonik API rotaları [hierarchy.md](hierarchy.md) içindedir.

# 02 — Site API ve ekran sözleşmesi

Yerel API: `http://localhost:5005`. Tüm isteklerde `Authorization: Bearer <access token>` gerekir. Token Keycloak Authorization Code + S256 PKCE ile alınır; parola grant kullanılmaz.

| İşlem | Endpoint | Yetki / sonuç |
| --- | --- | --- |
| Oluşturma | POST /api/v1/sites | SUPER_ADMIN; 201 + Location ve SiteResponse |
| Liste | GET /api/v1/sites | SUPER_ADMIN tüm silinmemiş siteler; diğer kullanıcılar aktif üyelik kapsamı |
| Detay | GET /api/v1/sites/{guid} | Aynı veri kapsamı; bulunmayan/silinmiş/kapsam dışı kayıt 404 |

POST body:

```json
{"name":"Çınar Yaşam Sitesi","city":"İstanbul","district":"Kadıköy","address":"Mahalle, sokak, kapı numarası"}
```

Tüm alanlar zorunlu ve trim edilir. Uzunluk sınırları: name 200; city/district 100; address 500. Boş veya fazla uzun alanlar `application/problem+json` ValidationProblemDetails ile 400, `errors` içinde alan bazlı mesaj verir. Anonim/geçersiz JWT 401; oluşturma rolü veya sabit JWT sub bulunmazsa 403. CreatedBy, Version ve kimlik istemciden alınmaz.

SiteResponse alanları: `id`, `name`, `city`, `district`, `address`, `version`, `createdAt`, `isEnabled`. Oluşturma Site + oluşturanın SiteMembership kaydını tek transaction ile yazar.

Liste query örneği:

```text
/api/v1/sites?page=1&pageSize=10&search=Kad%C4%B1k%C3%B6y&sortBy=name&sortDirection=asc
```

- page: 1–1000000; pageSize: 1–100 (API varsayılan 12; tablo varsayılan 10).
- search: en fazla 200 karakter; site adı, şehir veya ilçede arama. `%`, `_` ve `\` literal karakterlerdir.
- sortBy: `name`, `city`, `createdAt`; sortDirection: `asc`, `desc`. Geçersiz değerler 400. Eşit değerlerde Id kararlı sıralama sağlar.
- Sonuç: `{ "items": [...], "page": 1, "pageSize": 10, "totalCount": 1 }`. Count ve tüm filtreler erişim kapsamının içinde hesaplanır.

Keycloak rollerinin tanımlanması tek başına üyelik sağlamaz. SITE_ADMIN/MANAGER/ACCOUNTANT/RESIDENT kullanıcıları aktif, silinmemiş SiteMembership olmadan kayıt göremez. SUPER_ADMIN global erişimlidir. Üyelik atama UI/API'si sonraki kapsamda; bu endpointler istemciye scope veya userId seçtirmez.

Frontend: `/sites` shadcn/ui + TanStack Table; modal ekleme, sunucu araması/sıralaması/sayfalaması, sütun görünürlüğü ve `/sites/{guid}` detay. Hassas veri anonim SSR içinde yoktur. OIDC root callback sonrası yalnızca doğrulanan yerel site adresine dönülür; tokenlar bellekte kalır.

Güncelleme/silme, blok/daire yönetimi ve 409 form akışı 03 aşamasında planlıdır. Doğrulamalar: [TESTING.md](../TESTING.md).
