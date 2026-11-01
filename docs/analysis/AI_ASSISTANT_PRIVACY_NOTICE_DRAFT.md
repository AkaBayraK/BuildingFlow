# Aydınlatma metni — yapay zekâ asistanı maddesi (TASLAK)

> **Durum: TASLAK — yayınlanmadı.** Bu metin rıza/aydınlatma modülüne yeni sürüm olarak eklenmedi ve
> kullanıcılara gösterilmiyor. Yayından önce KVKK danışmanı tarafından incelenmeli; yöneticinin kendi
> kurumu adına veri sorumlusu bilgileriyle tamamlanmalıdır. Hukuki görüş değildir.
>
> Kapsam: AI Asistan FAZ 1 — yalnız yönetici, site yöneticisi, süper yönetici ve muhasebeci rolleri.
> Sakin/malik tarafı (analizdeki Faz 3) için ayrı rıza türü ve ayrı metin gerekir; bu taslak onu kapsamaz.

## Önerilen madde (Türkçe)

**Yapay zekâ destekli yönetim özeti asistanı**

BuildingFlow yönetim panelindeki "AI Asistan", yetkili yöneticilere yönetim alanının günlük durumu, aidat
tahsilatı, gider-bütçe karşılaştırması, planlı bakımlar ve açık talep sayıları hakkında özet sunar.

1. **İşlenen veriler:** Asistan, yöneticinin zaten görmeye yetkili olduğu ekranlardaki verilerden yalnız
   toplu sayıları, tutar toplamlarını, tarihleri ve kategori kodlarını kullanır. Malik, kiracı veya sakinlerin
   adı, telefonu, e-postası, kimlik bilgileri, hangi dairede oturduğu, kişi bazında borç bilgisi ve talep
   açıklamaları asistana ve yapay zekâ modeline **aktarılmaz**.
2. **Amaç ve hukuki sebep:** Site/apartman yönetiminin yürütülmesi ve yöneticinin karar desteği
   (KVKK m.5/2-f meşru menfaat; yönetim hizmet sözleşmesinin ifası). Asistan kayıt oluşturmaz, değiştirmez,
   ödeme almaz veya ileti göndermez; tüm kararlar yöneticiye aittir.
3. **Yapay zekâ hizmeti:** Özet metni, yapılandırılmışsa veri sorumlusunun yetkilendirdiği AkaAI hizmeti ile
   oluşturulur. Bu hizmete yalnız 1. maddedeki toplu veriler ve yöneticinin yazdığı soru metni iletilir.
   AkaAI'nin barındırıldığı yer ve yurt dışına aktarım durumu: **[doldurulacak — yurt içi/yurt dışı;
   yurt dışıysa KVKK m.9 dayanağı (yeterlilik kararı / standart sözleşme ve Kurul'a bildirim)]**.
   Model kullanılamadığında özet, aynı veriler üzerinden kural tabanlı olarak üretilir.
4. **Kullanıcı (yönetici) verisi:** Kötüye kullanımı önlemek ve hizmet güvenliği için hangi kullanıcının,
   hangi yönetim alanında, ne zaman, hangi araçla ve ne kadar sürede soru sorduğu kayıt altına alınır.
   **Soru metni ve asistanın cevabı saklanmaz ve loglanmaz.** Kullanıcı başına günlük soru sınırı uygulanır.
   Saklama süresi: **[doldurulacak — ör. audit kayıtları için kurum saklama politikası]**.
5. **Uyarı:** Yöneticiler soru alanına kişi adı, telefon veya başka kişisel veri yazmamalıdır; yazılan soru
   metni özet üretimi için yapay zekâ hizmetine iletilir (saklanmaz).
6. **Haklar:** İlgili kişiler KVKK m.11 kapsamındaki haklarını **[veri sorumlusu iletişim bilgisi]**
   üzerinden kullanabilir.

## Proposed clause (English)

**AI-assisted management summary assistant**

The "AI Assistant" in the BuildingFlow management panel gives authorised managers a summary of their
management area: daily status, dues collection, expenses against budget, planned maintenance and open request
counts. It uses only aggregate counts, totals, dates and category codes from screens the manager may already
see. Names, phone numbers, e-mail addresses, identity data, unit-person links, per-person balances and request
descriptions are **not** passed to the assistant or to the AI model. The assistant never creates or changes
records. If configured, the summary text is produced by the controller's authorised AkaAI service
**[hosting location and any cross-border transfer basis to be completed]**; otherwise it is produced by
deterministic rules. Who asked, when, in which area, with which tool and how long it took is recorded;
**questions and answers are not stored or logged**. Managers must not type personal data into the question box.

## Yayın öncesi kontrol listesi

- [ ] KVKK danışmanı görüşü
- [ ] AkaAI barındırma yeri / aktarım dayanağı
- [ ] Audit saklama süresi
- [ ] Rıza/aydınlatma modülünde yeni metin sürümü (mevcut `consent_texts` akışı) — ayrı karar
- [ ] Sakin tarafı (Faz 3) için ayrı rıza türü — bu fazda yok
