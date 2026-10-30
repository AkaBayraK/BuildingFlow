# TEDARİKÇİ FAZ03 — Alan, kategori ve hizmet ilişkileri

Teslim29.10.2026; uygulama/test koşumları28.10.2026'da başlatıldı. Dal `buildingflow_tedarikci`, GitHub[#93](https://github.com/AkaBayraK/BuildingFlow/issues/93).

Kategori oluşturma UUID ile aynı normalize içeriği tekrar döndürür; tenant başına normalize ad tek. SupplierAreaService eklemeli migration; tenant+supplier+Property ve tenant+supplier+service compositeFK. Tedarikçi birden fazla yetkili alanda farklı/ortak hizmetlere bağlanır; aynı hizmet kaydı yeniden kullanılır. Tenant→Property kilidi, version ve UUID işlem geçmişi korunur. Aktif tedarikçi/alan/kategori ve alana özel yönetim ataması zorunludur. Liste sadece aktif izinli Property kapsamındadır; üyelik kaldırıldığında alan ID/adı yanıttan çıkar. Muhasebeci ve anonim erişim reddedilir.

Detay panelinde alan/hizmet listesi, yetkili Property seçimi, ikonlu kategori seçimi ve kategori oluşturma. İşlem sürerken dış panel kapatma/düzenleme kilitlenir.409 kör retry yapmaz. TR/EN; yeni paket yok.

## Kanıt

- Son `bash scripts/test.sh --filter 'Supplier|PersistenceTests|ErrorContractTests|QaEndpointInventoryTests|ManagementAssignmentTests'`: **Integration57/57+Unit5/5, skipped0** (`/tmp/bf-supplier03-tests3.log`). Yalnız geçici DB/rol; guard ve temizlik geçti. Yeni yönerge29.10.2026'da geldiğinde tamamlanan önceki koşumdur; yeniden çalıştırılmış diye sunulmaz.
- Kapsam: çoklu alan, hizmet dedup, eskiUUID güncelversion, aynıversion eşzamanlı tek kazanan, gerçek yabancı tenant kategori ve alan ret, üyelik kaldırılınca gizleme; rol/anonim/validation/pasif üst; model+gerçek şema audit, upgrade/repeat/emptyDown/reUp ve her iki doluDown guard, immutable operation.
- İlk ilişki sorgusu32 testten1FAIL (HTTP500): DTO constructor üzerinden OrderBy EF tarafından çevrilemedi. Sıralama anonim SQL projeksiyonuna taşındı; tekrar koşumu32/32, son geniş koşum57/57 geçti.
- Web **306/306+typecheck+production build** (`/tmp/bf-supplier03-webtests2.log`, `/tmp/bf-supplier03-typecheck3.log`, `/tmp/bf-supplier03-webbuild.log`). İlk roadmap çeviri namespace hatası düzeltildi; tekrar306/306 geçti.
- GerçekReact/sentetikAPI tarayıcı: aynı tedarikçiye FixtureAreaA+FixtureAreaB/hizmet, yeniTemizlik kategorisi ve iki ilişkili liste doğrulandı.29.10.2026'da son görünüm yeniden okundu ve[ekran kanıtı](evidence/03-two-areas.png) alındı. CanlıKeycloak/APIbrowserE2E değildir.
- Restore sonrası lisans metadata329PASS (`/tmp/bf-supplier-final-licenses.log`). Yeni paket/asset/container yok.

FAZ03 resmi2/2=%100,00; akış4/13=%30,77. CanlıDB migration/runtime/deploy yapılmadı. SonrakiFAZ04; kullanıcının devam talebi mevcut sırayı sürdürür.
