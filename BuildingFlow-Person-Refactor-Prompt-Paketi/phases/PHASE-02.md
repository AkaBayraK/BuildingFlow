# FAZ 02 — Hedef Veri Modeli ve Sözleşmeler

## Hedef ve görev
Analiz onayı sonrası Person bağımsız domain kaydı; User->Person; UnitOwnership (pay oranı/tarih), UnitTenancy, UnitResidency (fiili oturum), ManagementAssignment (alan/görev/tarih) modelini tasarla. Tenant-scope görünürlük, aynı kişinin farklı tenantlarda hukuka uygun eşleşmesi, person merge/dedupe ve ilişki geçerlilik kurallarını tanımla. Kişiye is_owner/is_tenant/is_manager boolean ekleme. Önce şema ve API tasarımını yaz; gerekiyorsa uygulama değişikliklerini ayrı onaya sun.

## Uygulama talimatı
1. `MASTER_PROMPT.md`, `STANDARDS.md`, önceki faz raporlarını ve ilgili mevcut kodu oku.
2. Bu fazın önkoşulları sağlanmıyorsa uygulama yapma; engeli raporla.
3. Önce kısa etki analizi ve dosya planını Türkçe konsola yaz.
4. Yalnızca bu fazın kapsamındaki işleri gerçekleştir. Kod değişikliği gerekiyorsa mevcut davranışı koruyarak küçük ve doğrulanabilir adımlarla ilerle.
5. Uygun testleri çalıştır; çalıştırılamayanları gerekçelendir.
6. `docs/refactor/person/phase-02-report.md` raporunu oluştur; kullanıcı onayını bekle.

## Tamamlanma kriteri
- Kapsam dışı kod değişmedi; veri kaybı veya yetki genişlemesi yok.
- İlgili test ve güvenlik kontrol sonuçları raporlandı.
- Türkçe konsol ilerleme ve değişen dosya listesi verildi.
- Sonraki faz kendiliğinden başlatılmadı.
