# TEDARİKÇİ FAZ01 — Veri modeli ve yetkilendirme

28.10.2026; dal `buildingflow_tedarikci`; GitHub [#91](https://github.com/AkaBayraK/BuildingFlow/issues/91).

Supplier, SupplierCategory, SupplierService, SupplierManagementArea, SupplierContact, SupplierDocument ve UUID SupplierOperation eklendi. Hepsi ortak BaseEntity audit/version/soft-delete mapping kullanır. Tenant+supplier/category, tenant+Property ve Property+StoredDocument composite FK; normalize ad benzersizliği; gerekçeli INACTIVE/BLOCKED; immutable operation trigger ve dolu Down guard. Vergi/TCKN/IBAN bu temel kayıtta toplanmaz. Belge metadata'sı mevcut özel StoredDocument'a bağlıdır; yükleme/sözleşme FAZ04.

`/api/v2/tenants/{tenantId}/suppliers` GET liste/GET detay/POST/PUT: sadece SUPER_ADMIN/SITE_ADMIN/MANAGER; aktif tenant, issuer+subject hesabı ve üyelik; ENFORCED atama kararı korunur. Tenant kilidiyle version/409 ve normalize UUID fingerprint. Eski UUID güncel kaydı döndürür; sessiz geri dönüş yok. ACCOUNTANT ve sakin kişisel iletişim okuması açılmadı. Alan ilişki mutation/UI FAZ03, liste/form UI FAZ02.

## Bu tur kanıtları

- `.NET build --no-restore -m:1 -nr:false`: 0 hata/0 uyarı (`/tmp/bf-supplier-build3.log`).
- `bash scripts/test.sh --filter 'Supplier|PersistenceTests|MigrationTests.Model|ErrorContractTests|QaEndpointInventoryTests'`: Integration29/29, Unit2/2, skipped0. Yalnız geçici PostgreSQL/rol; guard geçti, kaynaklar temizlendi (`/tmp/bf-supplier-test2.log`). İlk koşum test Actor.Username derleme hatası verdi; düzeltildi ve son koşum tamamen tekrarlandı.
- Kapsam: manager roller,401/403, tenant/issuer/revoked membership404, ENFORCED atamasız ret, duplicate, literal arama/sayfalama,400; eşzamanlı UUID replay/version tek kazanan, güncel replay; upgrade/repeat/emptyDown/reUp/doluDown; tenant FK/immutable receipt ve model/gerçek tüm tablo sekiz NOT NULL audit sözleşmesi.
- Web `npm test`301/301, typecheck ve production build geçti (`/tmp/bf-supplier-webtest.log`, `/tmp/bf-supplier-typecheck.log`, `/tmp/bf-supplier-webbuild.log`). Yeni iki hata kodu TR/EN registry ile eşleşir.
- Restore sonrası `python3 scripts/check-licenses.py`:329 PASS. Yeni paket/asset/font/container yok. `git diff --check` geçti.

FAZ01'de ekran yok; UI E2E uygulanamaz. Migration yerel uygulama DB'sine uygulanmadı; API yeniden başlatılmadı; mobil/deploy/CI yapılmadı. Bu temel veri/API fazı tüm sonraki ekranları tamamlanmış göstermez. Resmi3/3=%100,00; akış2/13=%15,38. Sonraki FAZ02, kullanıcının mevcut sıralı geliştirme yetkisiyle.
