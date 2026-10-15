# Mobil finans — Faz11

15.10.2026. GET `/api/v1/mobile/finance?propertyId=<uuid>&role=<role>&view=<charges|payments>&page=<1..10000>[&unitId=<uuid>][&chargeId=<uuid>]`. Sadece GET; ödeme mutation/sağlayıcı bağlantısı yok. Mevcut center Payment idempotency/tersleme sözleşmesi değişmedi.

OWNER/TENANT linked aktif Person,geçerli Occupancy,aktif PropertyMembership/Property/Unit/Building ile doğrulanır; gerçek resident ilişkisi realm claim yerine geçer. Yalnız UNIT+ownrole/person tahakkukları. Diğer kişisel borç ve allocation gizlidir. SUPER_ADMIN/SITE_ADMIN/MANAGER/ACCOUNTANT için seçili gerçek JWT rolü ve mevcut Property kapsamı; global SUPER_ADMIN istisnası dışında aktif üyelik. Yönetim bağlamında unitId reddedilir. chargeId her seferinde yetkili sorguda aranır; yabancı/bilinmeyen tahakkuk403,anonim401,bozukpage/view400.

FinanceSnapshot: userId/contextKey/asOf/propertyName/view/page/chargeId/total/summary/charges/payments. Summary amount/paid/balance/overdue; stringTRYiki ondalık,iptal edilmemiş tüm yetkili borçlar,detay ve sayfadan bağımsız. Charges: OwnerCharge+unitNumber. Payments: allocationId,chargeId,unitNumber,period,amount,paidDate,cancelled. Kayıtlar banka transferi kanıtı değildir; terslenenler geçmişte korunur,güncel paid tutarına katılmaz. Banka ref/açıklama,payer,Payment toplamı,kişi adı/kimlik/iletişim dönmez. ACCOUNTANT'a kişi yetkisi açılmaz.

RepeatableRead,Europe/Istanbul gün,50sabit sayfa,kayıtID ile deterministic tie-breaker; sayfalar arasında değişen veride yenileme gerekir. Desteklenen500000liste kaydı sınırı toplamları etkilemez. Yeni şema/migration yok. MobileOwnerStore ile kişisel borç filtresi MobileFinanceQueries.ResidentCharges üzerinden ortak; önceki owner/tenant DTO davranışı korunur.

Tüketici: kardeş buildingflow-mobile/mobile/src/finance ve FinancePanel. Hesap/context/view başına son sayfa,chargeId payload doğrulaması,24saatşifrelicache,403purge,geçyanıt ayrımı;webRAM. Kart verisi alınmaz. Online ödeme sağlayıcısı/yönlendirme sözleşmesi bulunmadığından ödeme başlatma eklenmedi.

Bu tur:54/54mobil,42/42gerçekayrıPGintegration(MobileFinance/Manager/Owner/Tenant/Session/PaymentApi),8/8PaymentDomain;atlama0. TypeScript,lint0hata1öncekiuyarı,3platformexport,build0hata0uyarı. Provider başarısız/tekrar ödeme UI testlenmiş sayılmaz; mevcut merkez ödeme idempotency/overpayment/tersleme testleri geçti. Gerçek iPhone/Android görsel/offline kabulü açık. Ayrıntı kardeş docs/mobile/PHASE11.md.
