# Faz 11 — Aidat, borç, ödeme ve işlem geçmişi

15.10.2026. Kullanıcı Faz11'i açıkça başlattı. Kanonik PHASES.md kapsamındaki finans okuma akışı uygulandı; Faz12 başlamadı.

## Uygulanan deneyim

Ortak Aidat ve ödemeler ekranı OWNER/TENANT/SUPER_ADMIN/SITE_ADMIN/MANAGER/ACCOUNTANT bağlamında gerçek merkez verisini kullanır. Lacivert bakiye kartı, tahakkuk/mahsup/kalan/gecikmiş toplamları, son güncelleme ve iki sekme.50satırlık sunucu sayfalamasıyla tüm yetkili dönem ve mahsup geçmişi erişilebilir. Dönem kartından ilgili tahakkukun mahsuplarına, mahsuptan tahakkuka geçiş ve tüm dönemlere dönüş. Genel özet sayfa/detay filtresinden bağımsız bütün yetkili tahakkukları kapsar; iptal edilmiş tahakkuklar toplam dışıdır. Liste, iptal ve tersleme geçmişini açık durumla gösterir.

Ödeme sağlayıcısı/yönlendirme sözleşmesi merkezde mevcut değildir. Kart girişi, ödeme başlatma ve sahte başarı eklenmedi. PaymentAllocation merkez tahsilatının ilgili borca mahsup edilen kısmıdır; banka transferi teyidi değildir. Terslenen kayıtlar güncel ödenen tutara dahil değildir. Bu tur yeni finans yazımı veya istemci ödeme kuyruğu yok; mevcut ödeme idempotency/tersleme kuralları değişmedi ve regresyonları çalıştırıldı.

## Yetki ve veri sözleşmesi

GET `/api/v1/mobile/finance?propertyId=<uuid>&role=<role>&view=<charges|payments>&page=<1..10000>[&unitId=<uuid>][&chargeId=<uuid>]`.

OWNER/TENANT: gerçek linked Person+bugün geçerli aktif ilişki+aktif üyelik/Property/Unit/Building; UNIT ve bu role/kişiye açık atanmış borçlar. Diğer malik/kiracı kişisel borcu gizlidir. Yönetim/muhasebe: seçilen gerçek JWT rolü; SUPER_ADMIN mevcut global aktif Property sınırı, diğerleri aktif üyelik. Yönetici için unitId verilmez. RESIDENT tek başına finans yetkisi vermez. Her chargeId ayrıntı isteği aynı borç filtresinden geçer; anonim401,yabancı/revoked/yanlışrol403,bozukpage/view400. RepeatableRead; Europe/Istanbul iş günü.

Üretici DTO/store/endpoint Application/Infrastructure/API katmanlarında. MobileOwnerStore ile borç kapsamı MobileFinanceQueries.ResidentCharges üzerinden ortak; eski owner/tenant sözleşmeleri korunur. Tahsilat DTO yalnız allocationId/chargeId/daire/dönem/tutar/tarih/terslenme taşır; toplam Payment, bankareferansı/açıklaması, payer, kimlik/telefon/email ve kişi adları dönmez. ACCOUNTANT kişi yetkisi genişletilmez.

## Yerel veri ve ortak kod

Schema5owner_snapshots şifreli tablosu yeniden kullanıldı; user+property+unit+role+finance+view. Bağlam başına yalnız iki son sayfa slotu; sayfa ve chargeId payload'da kontrol edilir, yanlış detay çevrimdışı gösterilmez.24saatTTL,backwards-clock,eski sunucu yanıtı reddi,whitelist,403iki görünümü purge; mevcut logout anahtar/DB temizliği. Web RAM. Yönetici ve finans yükleme yaşam döngüsü ortak useScopedRead hook'una alındı: scope değişince eski veri hemen gizlenir, geç yanıt yazılmaz; bozuk cache temizlenip online yeniden alınabilir.

Para, tarih ve tahakkuk parser'ı owner modülüyle ortaklaştırıldı. Ondalıklı tutarlar string, doğrulama BigInt kuruş cinsindendir; float yuvarlama yok. Tutar toplamı/paid/balance ve PAID/CANCELLED/OPEN/PARTIALLY_PAID/OVERDUE çelişkileri reddedilir. Uygulama tutar girişinden ödeme başlatmaz.

## Bu tur doğrulama ve sınırlar

-54/54 mobil test. Yeni6grup: hassas/çokbüyük tutarlar; foreign hesap/rol/page/detail; tersleme; cache whitelist/sonsayfa/rol/detail; TTL/clock/eskiyanıt; sahte PAID/tutar durumu. Eski manager/owner/tenant/cache/sync/profile regresyonları geçti.
-42/42 gerçek ayrı PostgreSQL integration: MobileFinance6,MobileManager/Owner/Tenant/Session ve PaymentApi testleri;8/8 PaymentDomain.0atlama; geçici DB/role temizlendi. Kapsam: gerçek rol/üyelik/foreignscope/revocation,owner/tenantkişiselborç ve allocationgizliliği,detayIDOR,53tahakkuk/54mahsupsayfalaması ve tüm kapsam toplamları; ödeme idempotency/overpayment/tersleme regresyonları.
-TypeScript,lint0hata1öncekiuseOwnercleanupuyarısı,üçplatformexport,merkezbuild0hata0uyarı. GitHub sync script7test.
-Yeni dependency, DBmigration veya native modül yok; iPhone binary değişmedi. Canlı kullanıcı borç/tahsilat/rol verisi yazılmadı; commit/push/mağaza yayını yapılmadı.
-Kullanıcının önceki “bakmana gerek yok” talimatıyla görsel inceleme yapılmadı; gerçek iPhone/Android detay/geçmiş/offline kullanıcı kabulü açık. Otomatik testler bu kabul yerine sunulmaz. GitHub Faz11 In review; provider bağlantısı ayrıca açık madde.

Dosyalar: mobile/src/finance/*,components/FinancePanel.tsx,data/useScopedRead.ts,manager/useManager.ts,owner/contracts.ts,auth/AuthProvider.tsx,finance sekmesi ve testler. Merkez MobileFinance DTO/query/store/endpoint/DI/Program,MobileOwnerStore ve integration testi. API belgesi: kardeş BuildingFlow/docs/api/mobile-finance.md.
