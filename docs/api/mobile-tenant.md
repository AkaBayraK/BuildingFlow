# Mobil kiracı — Faz09

14.10.2026. GET `/api/v1/mobile/tenant?propertyId=<uuid>&unitId=<uuid>` JWTsub+linkedPerson+aktif/geçerliTENANTOccupancy+üyelik/property/unit/building. Rol claim’i bağı ikame etmez. Anonim401; scope/iptal üyelik/sona ermiş ilişki403. Mevcut IMobileOwnerStore/MobileOwnerStore ReadTenantAsync ortak ReadScoped üzerinden; mükerrer persistence yok. RepeatableRead; merkezi entity/migration yok.

OwnerSnapshot ile aynı ortak okuma alanları, contextKey property:unit:TENANT. Tenants yalnız bu hesaba bağlı kiracı adı/ilişki tarihleri; diğer kiracı/malik PII yok. Debt UNIT unassigned veya explicitTENANT debtor linkedownPerson. Owner/coTenant kişiselborç yok. Balance/overdue tüm yetkili satırlardan, son50charge;10ilişki/20duyuru-talep/morebayrakları. Duyuru PUBLISHED/ALL_MEMBERS; request ownsub/TENANT/unit.

Additive Payments/morePayments: son50PaymentAllocation yalnız authorizedcharge/property/unit; id, decimal0.00amount,paidDate,cancelled. PaymentReference/Description/fullPaymentAmount/payer bilgileri yok. Terslenme geçmişi korunur, charge netPaid/balance ile karıştırılmaz. OWNER endpoint boşPayments+false ile önceki davranışı korur; eski owner cache parser default[]; TENANT alanları zorunlu. Kira/kontrat/onlineödeme endpoint’i veya fakepaymentreceipt yok.

Native metinli talep mevcut `/api/v1/mobile/sync/commands`: TENANT/property/unit/CREATE/version0,1–2000description,stableoperationUUID/recordUUID; SessionStore scope/UUIDreceipt/fingerprint/version yarış kuralları korunur. Mobil enqueue başarısını sunucu başarısı gibi göstermez. ŞifreliSQLiteoutbox, stableIDretry/receipt, mevcutSyncPanel. Web önizleme talep yazmaz. Fotoğraf/kategori/managementstate12kapsamında.

Ortak SQLCipher schema5 cache tablosu owner_snapshots userId/contextKey (OWNERvsTENANT dahil);24saatTTL/403purge/context-generationguard/logoutcleanup. Web RAM. Yeni dependency/migration yok.

63/63 gerçek izolePostgreSQL integration (Tenant/Owner/Session/Sync/Authentication/ReleaseSecurity);45/45mobil+lint/typecheck/üçplatformexport. Kiracı/owner/coTenantcharge,Allocationprivacy/terslenme,anonim/forgedrole/diğerunit/property,revokedmembership/expiredTENANTsnapshot+requestreplay. YerelAPI/workerDockerbuild/deploy,health200/anonimtenant401/Metro200. Kullanıcı talimatıyla görsel/gerçekkişiselveriyazımı yok; fizikselUI/uçakmodu bu tur doğrulanmadı. Ayrıntı kardeş mobil docs/mobile/PHASE09.md.
