# FAZ 07 — Merkez Giriş ve Çalışma Alanları

## Hedef ve görev
Keycloak login sonrası .NET API tarafından kullanıcıya ait yönetim görevleri ve daire ilişkilerinden context listesi üret. Tek context varsa doğrudan aç; birden fazlaysa alan/ilişki seçtir. API her istekte sunucu tarafında scope ve nesne yetkisini kontrol et; menü gizlemek yeterli değildir.

## Uygulama talimatı
1. `MASTER_PROMPT.md`, `STANDARDS.md`, önceki faz raporlarını ve ilgili mevcut kodu oku.
2. Bu fazın önkoşulları sağlanmıyorsa uygulama yapma; engeli raporla.
3. Önce kısa etki analizi ve dosya planını Türkçe konsola yaz.
4. Yalnızca bu fazın kapsamındaki işleri gerçekleştir. Kod değişikliği gerekiyorsa mevcut davranışı koruyarak küçük ve doğrulanabilir adımlarla ilerle.
5. Uygun testleri çalıştır; çalıştırılamayanları gerekçelendir.
6. `docs/refactor/person/phase-07-report.md` raporunu oluştur; kullanıcı onayını bekle.

## Tamamlanma kriteri
- Kapsam dışı kod değişmedi; veri kaybı veya yetki genişlemesi yok.
- İlgili test ve güvenlik kontrol sonuçları raporlandı.
- Türkçe konsol ilerleme ve değişen dosya listesi verildi.
- Sonraki faz kendiliğinden başlatılmadı.
