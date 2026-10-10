# UI Aşama 06 — Daireler ve Kişiler

Teslim: 2026-10-09. Kullanıcının UI Faz 06 ile devam talebiyle, mevcut API kapsamındaki görsel yenileme uygulandı. İş yol haritasındaki ödeme/tahsilat fazı bu aşamadan ayrıdır. İlk analiz raporu tarihsel keşif kaydı olarak korunur.

## Referansların uygulanması

| Ekran | Uygulanan tasarım | Mevcut veri ile sınır |
| --- | --- | --- |
| Daire Yönetim Paneli | Lacivert başlık, açık kartlar, gerçek bölüm/kat/tür özeti; kompakt numara/kat/alan/durum tablosu, arama/sıralama/temizle/sayfalama, ikonlu CRUD; mobil kart ve sağ detay/düzenleme paneli | Dolu/boş KPI, oda planı 3+1, otopark ve sakin sayısı Unit DTO'sunda yok. Aktif/pasif doluluk değildir. Bina kapsamı mevcut seçim rotalarıyla korunur. |
| Sakinler Yönetim Paneli | Kişi kimlik harfleri, iletişim ikonları, durum ve devam eden ilişki sayısı; sağ panelde güncel iletişim ve tarihli malik/kiracı/sakin ilişkileri; ayrı ilişki yönetimi | Kişiye tek rol verilmez. Aile, TC, doğum/gender, notlar, kişiye göre bina/daire filtresi ve export sözleşmesi yok. Referanstaki örnek kişi ve istatistikler kopyalanmadı. |

Referans fotoğrafı, stok avatar, font veya ücretli template eklenmedi. Başlıktaki dekor mevcut Lucide ikonuyla React içinde çizilir. Global sidebar/header ve light/dark/system ayrı görsel aşamalardır; bu teslimle tamamlanmış sayılmaz.

## Ortak bileşenler ve dosyalar

- `frontend/buildingflow-web/src/components/management/WorkspacePresentation.tsx`: WorkspaceHeading, SummaryCard, IdentityMark, ListToolbar, DetailField ve ikonlu StatusBadge; API erişimi içermez.
- `frontend/buildingflow-web/src/components/management/RecordList.tsx`: aynı TanStack tablo/mobil kart; isteğe bağlı kompakt satır, manuel sunucu sıralaması ve footer kontrolleri.
- `frontend/buildingflow-web/src/components/ui/dialog.tsx`: merkez varsayılanı korunarak sağ panel yerleşimi; Radix focus/escape/overlay ve kapanış odağı.
- `frontend/buildingflow-web/src/features/hierarchy/HierarchyRows.tsx`: Building/Unit ortak liste, mevcut sunucu sorguları ve CRUD akışı.
- `frontend/buildingflow-web/src/features/hierarchy/RecordDetails.tsx`: yetkili güncel Unit/Person GET ve kişi ilişki önizlemesi. API kullanan iş akışı feature katmanındadır.
- `frontend/buildingflow-web/src/features/hierarchy/EntityEditor.tsx`: aynı validasyon, gönderim kilidi ve version/409 akışıyla Unit/Person sağ paneli; silme onayı merkezde kalır.
- `frontend/buildingflow-web/src/features/people/PeopleWorkspace.tsx`, `PersonPage.tsx`, `PersonOccupancies.tsx`: yeni liste/header, gerçek kişi ilişkileri ve ilişki sağ paneli.
- `frontend/buildingflow-web/src/features/navigation/ManagementPage.tsx`: gerçek kapsam/metadata özetleri; apartmanda doğrudan daire, diğer tiplerde mevcut bina seçimi.
- `frontend/buildingflow-web/src/services/management-contracts.ts`, `tests/management.test.mjs`: yeni dış Unit/Person/Occupancy response scope/shape doğrulaması ve beş negatif/pozitif sözleşme testi.
- `frontend/buildingflow-web/messages/{tr,en}.json`, `src/features/roadmap/Roadmap.tsx`: TR/EN yeni metin/ARIA ve ayrı, tikli/koyu UI 06 test kanıtı.

## İş kurallarının korunması

Property → opsiyonel Building → Unit; APARTMENT doğrudan Unit taşır. Sahte blok veya ayrı Floor/Section oluşturulmadı. Kişi API'si üç yönetici rolünde kaldı; ACCOUNTANT'a kişi erişimi açılmadı. Backend aktif üyelik ve rol kapsamının otoritesidir. Hassas veriler client-side merkezi yetkili API üzerinden alınır. Token/parola veya kişi araması URL/localStorage/log'a yazılmaz.

Yeni detay panelleri güncel kaydı yükler ve ebeveyn kapsamını doğrular. Düzenleme paneline güncel version aktarılır. Mevcut EntityEditor/DeleteEntity alan whitelist'i, busy kilidi, conflict yükleme ve geçmişi koruyan silme/bitiş işlemleri yeniden kullanılır. Daire aktifliği ile doluluk ve kişi kaydı ile giriş hesabı birbirine karıştırılmaz.

## Doğrulama ve gerçek sınırlamalar

**23 frontend test, typecheck ve production build başarılı. 312 paket sürümü lisans metadata kontrolü başarılı; yeni bağımlılık yok.** CUA ile mevcut apartmandaki 48 daire, gerçek kişi/malik ilişkisi, arama/sıralama/sayfalama, form hataları/iptal/odak, üç ilişki türü ve tarihler, TR/EN ve 390 px mobil görünüm doğrulandı. Kullanıcı kayıtlarına başarılı create/update/delete gönderilmedi. Anonim SSR kontrolü ve API/PostgreSQL health 200 geçti. `.env`, npm manifest/lockfile ve backend kaynak hash'leri değişmedi.

Site/bina ve pasif üst dalı kaynak koddan incelendi; bu tur tarayıcı kabulü mevcut apartmanla yapıldı. Yeni mutation, iki oturum 409 ve beşten fazla ilişkide panel pagination manuel yeniden çalıştırılmadı. Önceki backend/Keycloak testleri bu teslimde yeniden çalıştırılmış sayılmaz. FE/API/Worker Docker imajı yeniden derlenmedi. Yeni endpoint/migration/DB şeması eklenmedi.

Ayrıntılı sonuçlar ve ekranlar: [TESTING.md](../TESTING.md). Sonraki görsel aşama UI 07, kullanıcı tarafından ayrıca başlatılır. Eksik agregasyon/filtre/export gereksinimleri ayrı API tasarımı gerektirir.

## Kullanıcı düzeltmesi — 2026-10-09: ortak sağ panel

Kişilerdeki sağ panel formatı bütün yönetim kayıt formlarına genişletildi. Yeni components/management/ManagementPanel mevcut DialogContent üzerinde tek genişlik, yön ve başlık/alt eylem düzeni sunar; EntityEditor ve Property oluşturma, ilişki oluşturma/bitiş, aidat tahakkuk/detay ve RecordDetails bunu kullanır. Silme onayı ortak merkez diyaloğudur. Kapanış odağı her açılışta yakalanır. Test kanıtları ve canlı bina/borç kaydı bulunmaması sınırı TESTING.md’de; API/DB değişikliği veya yeni bağımlılık yok.
