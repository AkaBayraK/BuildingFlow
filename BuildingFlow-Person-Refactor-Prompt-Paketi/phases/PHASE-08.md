# FAZ 08 — Mobil Yerel Kişi ve Rol Modeli

## Hedef ve görev
Mevcut mobil framework/SQLite çözümünü tespit et. Merkezi Person/User/context/role/permission ilişkilerinin minimum gerekli alanlarını şifreli yerel depoya senkronize et. İlk giriş online Keycloak; sonraki offline giriş cihaz güvenli anahtar/biometri veya güvenli offline PIN ile; Keycloak şifresini SQLite’a kaydetme. Yetki snapshot süresi, cihaz kaybı ve iptal politikalarını uygula.

## Uygulama talimatı
1. `MASTER_PROMPT.md`, `STANDARDS.md`, önceki faz raporlarını ve ilgili mevcut kodu oku.
2. Bu fazın önkoşulları sağlanmıyorsa uygulama yapma; engeli raporla.
3. Önce kısa etki analizi ve dosya planını Türkçe konsola yaz.
4. Yalnızca bu fazın kapsamındaki işleri gerçekleştir. Kod değişikliği gerekiyorsa mevcut davranışı koruyarak küçük ve doğrulanabilir adımlarla ilerle.
5. Uygun testleri çalıştır; çalıştırılamayanları gerekçelendir.
6. `docs/refactor/person/phase-08-report.md` raporunu oluştur; kullanıcı onayını bekle.

## Tamamlanma kriteri
- Kapsam dışı kod değişmedi; veri kaybı veya yetki genişlemesi yok.
- İlgili test ve güvenlik kontrol sonuçları raporlandı.
- Türkçe konsol ilerleme ve değişen dosya listesi verildi.
- Sonraki faz kendiliğinden başlatılmadı.
