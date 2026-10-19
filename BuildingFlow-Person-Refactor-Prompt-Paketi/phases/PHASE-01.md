# FAZ 01 — Mevcut Person Yapısı Analizi

## Hedef ve görev
Salt-okunur repo keşfi: Person/User, ManagementArea, Block, Unit, EF Core entity/config/migration, API DTO, servis, UI, mobil ve SQLite ilişkilerini; tenant ve kişi erişim sınırlarını haritala. Mevcut veri kopyalarını ve olası eşleşme anahtarlarını incele. Kod/şema değiştirme. Hedef ERD, etki matrisi, veri migrasyon/rollback ve test planını raporla.

## Uygulama talimatı
1. `MASTER_PROMPT.md`, `STANDARDS.md`, önceki faz raporlarını ve ilgili mevcut kodu oku.
2. Bu fazın önkoşulları sağlanmıyorsa uygulama yapma; engeli raporla.
3. Önce kısa etki analizi ve dosya planını Türkçe konsola yaz.
4. Yalnızca bu fazın kapsamındaki işleri gerçekleştir. Kod değişikliği gerekiyorsa mevcut davranışı koruyarak küçük ve doğrulanabilir adımlarla ilerle.
5. Uygun testleri çalıştır; çalıştırılamayanları gerekçelendir.
6. `docs/refactor/person/phase-01-report.md` raporunu oluştur; kullanıcı onayını bekle.

## Tamamlanma kriteri
- Kapsam dışı kod değişmedi; veri kaybı veya yetki genişlemesi yok.
- İlgili test ve güvenlik kontrol sonuçları raporlandı.
- Türkçe konsol ilerleme ve değişen dosya listesi verildi.
- Sonraki faz kendiliğinden başlatılmadı.
