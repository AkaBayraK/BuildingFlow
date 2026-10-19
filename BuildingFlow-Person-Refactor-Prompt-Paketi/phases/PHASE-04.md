# FAZ 04 — Daire İlişkileri ve Geçiş Migrasyonu

## Hedef ve görev
UnitOwnership, UnitTenancy, UnitResidency ilişkilerini uygula; tarih aralıkları, ortak mülkiyet, birden çok malik, kiracı/sakin ayrımı ve çakışma kurallarını doğrula. Expand-migrate-contract yaklaşımı: önce yeni tablolar, sonra dry-run backfill, kayıt sayısı/foreign key karşılaştırması, ardından uyumlu okuma. Yıkıcı adımı ayrı onaya bırak.

## Uygulama talimatı
1. `MASTER_PROMPT.md`, `STANDARDS.md`, önceki faz raporlarını ve ilgili mevcut kodu oku.
2. Bu fazın önkoşulları sağlanmıyorsa uygulama yapma; engeli raporla.
3. Önce kısa etki analizi ve dosya planını Türkçe konsola yaz.
4. Yalnızca bu fazın kapsamındaki işleri gerçekleştir. Kod değişikliği gerekiyorsa mevcut davranışı koruyarak küçük ve doğrulanabilir adımlarla ilerle.
5. Uygun testleri çalıştır; çalıştırılamayanları gerekçelendir.
6. `docs/refactor/person/phase-04-report.md` raporunu oluştur; kullanıcı onayını bekle.

## Tamamlanma kriteri
- Kapsam dışı kod değişmedi; veri kaybı veya yetki genişlemesi yok.
- İlgili test ve güvenlik kontrol sonuçları raporlandı.
- Türkçe konsol ilerleme ve değişen dosya listesi verildi.
- Sonraki faz kendiliğinden başlatılmadı.
