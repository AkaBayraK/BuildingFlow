# FAZ 09 — Mobil Offline CRUD ve Senkronizasyon

## Hedef ve görev
Talep/arıza/fotoğraf ve uygun profil taslaklarını yerelde transaction + outbox ile kaydet. İdempotency key, retry/backoff, conflict, durum ekranı, manuel/otomatik sync, attachment güvenliği ekle. Sunucu senkronizasyonda yetkiyi tekrar doğrulasın; ödeme, kesin tahakkuk ve rol değişimi online zorunlu olsun. Offline veriyi kesinleşmiş sunucu kaydı gibi gösterme.

## Uygulama talimatı
1. `MASTER_PROMPT.md`, `STANDARDS.md`, önceki faz raporlarını ve ilgili mevcut kodu oku.
2. Bu fazın önkoşulları sağlanmıyorsa uygulama yapma; engeli raporla.
3. Önce kısa etki analizi ve dosya planını Türkçe konsola yaz.
4. Yalnızca bu fazın kapsamındaki işleri gerçekleştir. Kod değişikliği gerekiyorsa mevcut davranışı koruyarak küçük ve doğrulanabilir adımlarla ilerle.
5. Uygun testleri çalıştır; çalıştırılamayanları gerekçelendir.
6. `docs/refactor/person/phase-09-report.md` raporunu oluştur; kullanıcı onayını bekle.

## Tamamlanma kriteri
- Kapsam dışı kod değişmedi; veri kaybı veya yetki genişlemesi yok.
- İlgili test ve güvenlik kontrol sonuçları raporlandı.
- Türkçe konsol ilerleme ve değişen dosya listesi verildi.
- Sonraki faz kendiliğinden başlatılmadı.
