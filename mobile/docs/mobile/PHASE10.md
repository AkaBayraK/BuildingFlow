# Faz 10 — Yönetici deneyimi

15.10.2026. Kullanıcının “kalan fazdan devam … kodlar mısın” talimatıyla yalnız Mobil Faz10 uygulandı. Kanonik kaynak buildingflow-mobile-prompts/PHASES.md. Faz11 başlamadı.

## Teslim

- SUPER_ADMIN/SITE_ADMIN/MANAGER seçili bağlamında ana ekran artık gerçek yönetim özetini gösterir. Lacivert bina kartı, bina/blok–daire–aktif kişi kartları, yeşil aidat özeti ve duyuru/talep kısayolları BuildingFlow referansını izler. Hesabım’dan mevcut yetkili rol/mülk seçilir; kullanıcıya rol eklenmedi.
- Ana ekranda Genel bakış / Bina-blok / Daireler / Sakinler; Aidat / Duyuru / Talep sekmelerinde yönetici listeleri. Bina veya daire düzenleme, tahsilat, borç iptali, duyuru yayınlama ve talep durumu yazımı yok. Yönetici kapsamı güvenli, kritik okuma akışlarıyla sınırlandı; sahte başarı veya çalışmayan yazım düğmesi eklenmedi.
- Her liste merkezde deterministik sıralama ve sayfa başına50kayıt; önceki/sonraki sayfa. Sayfa birikimi yok; tek sayfa render edilir. Boş/hata/yükleniyor ve offline tarih görünür. Sakin sayısı benzersiz güncel aktif kişidir; sakin listesi kişi/daire/rol ilişkileridir, ortak malik/çoklu ilişki nedeniyle satır sayısı kişi sayısından farklı olabilir.
- Tüm binanın iptal edilmemiş aidat kalan/gecikmiş toplamı liste sayfasından bağımsızdır. Tarihsel tahakkuklar korunur. Duyurular taslak/yayın/arşiv ve MANAGEMENT içeriğiyle yetkili yöneticiye sunulur. Talepler yalnız merkezde kayıtlı MobileRequest verisidir; iş emri/atama/çözüm durumu uydurulmaz.

## Sözleşme ve güvenlik

GET `/api/v1/mobile/manager?propertyId=<uuid>&role=<management-role>&section=<section>&page=<1..10000>`; overview/buildings/units/residents/finance/announcements/requests. JWT sub ve seçilen gerçek realm rolü zorunlu. SUPER_ADMIN mevcut session sözleşmesindeki global aktif Property kapsamına sahiptir; SITE_ADMIN/MANAGER için aktif PropertyMembership şarttır. ACCOUNTANT kişi yetkisini genişletmemek için endpoint/UI kapsamına alınmadı. OWNER/TENANT kendi mevcut panellerini kullanır.

RepeatableRead snapshot; EF soft-delete filtreleri korunur. Aktif Property/bina/daire ve bugün Europe/Istanbul iki uç dahil aktif ilişkiler. Sakin özeti yalnız ad, daire ve rol içerir; kimlik/telefon/email/doğum/adres/hesap bağı dönmez. Ödeme payer/banka verisi yoktur. Auth401, yetki403; geçersiz section/page400. Yeni DB tablosu/migration/dependency yok.

SQLCipher schema5 owner_snapshots tablosu yeniden kullanılır; user+contextKey+manager+section cache anahtarı, bölüm başına yalnız son sayfa. Snapshot payload’da ayrıca sayfa doğrulanır; yanlış sayfa gösterilmez.24saatTTL/backward clock; eski sunucu yanıtı reddi, whitelist,403tüm seçili yönetim bölümlerini purge, hesap/rol/mülk değişiminde geç yanıt koruması. Web RAM kullanır. Çıkıştaki mevcut anahtar/DB temizliği korunur.

## Bu tur doğrulama

-48/48 mobil test: yeni3test grubu hesap/rol/mülk/section/page ayrımı; para/status/sayfa boyutu/tekrar UUID; whitelist,TTL,clock,son-sayfa,eski yanıt ve purge.
-20/20 gerçek izole PostgreSQL integration (MobileManager/Owner/Tenant/Session filtreleri),0atlama. Yeni4yönetici testi: summary/current residents/PII,51daire sayfalama,rol/üyelik/foreign scope/revocation,global admin/inactive Property/geçersiz sayfa. Geçici DB/role temizlendi; canlı iş kaydı mutation yok.
-TypeScript başarılı; lint0hata/1önceden var olan useOwner cleanup uyarısı. Dokunulan sekmelerdeki6duplicate-import uyarısı giderildi. Merkez build0hata/0uyarı. iOS/Android/Web export başarılı.
-Yeni native modül yok; yeni uygulama binary’si gerekmez. Mobil API ve Metro yeni kodla yeniden başlatıldı. Kullanıcının önceki “bakmana gerek yok” talimatıyla görsel/ekran incelemesi yapılmadı. Gerçek iPhone/Android UI ve offline kabulü doğrulanmadı; otomatik testler bu kabul yerine sunulmaz.

Değişiklikler: mobile/src/manager/*, components/ManagerPanel.tsx, auth/AuthProvider.tsx, dört sekme; merkez MobileManager DTO/store/endpoint/DI/Program ve entegrasyon testleri. Paket/lock/migration değişikliği yok. Yeni lisans yükümlülüğü eklenmedi. Commit/push/mağaza yayını yapılmadı. GitHub kartı cihaz kabulü açık olduğundan In review olarak izlenir.
