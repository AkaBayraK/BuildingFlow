# FAZ 03 — Person Backend Refactor

## Hedef ve görev
Person create/update/search/detail servis ve DTO yapılarını yönetim alanından bağımsız hale getir; tenant güvenliği, alan bazlı veri minimizasyonu ve erişim kontrollerini koru. Mevcut kayıt ve API tüketicilerini bozma; uyumluluk katmanı gerektiğinde ekle. Kişi doğrulama, iletişim benzersizliği iddialarını temkinli ele al; ortak telefon/e-posta ve mükerrer kişi senaryolarını test et.

## Uygulama talimatı
1. `MASTER_PROMPT.md`, `STANDARDS.md`, önceki faz raporlarını ve ilgili mevcut kodu oku.
2. Bu fazın önkoşulları sağlanmıyorsa uygulama yapma; engeli raporla.
3. Önce kısa etki analizi ve dosya planını Türkçe konsola yaz.
4. Yalnızca bu fazın kapsamındaki işleri gerçekleştir. Kod değişikliği gerekiyorsa mevcut davranışı koruyarak küçük ve doğrulanabilir adımlarla ilerle.
5. Uygun testleri çalıştır; çalıştırılamayanları gerekçelendir.
6. `docs/refactor/person/phase-03-report.md` raporunu oluştur; kullanıcı onayını bekle.

## Tamamlanma kriteri
- Kapsam dışı kod değişmedi; veri kaybı veya yetki genişlemesi yok.
- İlgili test ve güvenlik kontrol sonuçları raporlandı.
- Türkçe konsol ilerleme ve değişen dosya listesi verildi.
- Sonraki faz kendiliğinden başlatılmadı.
