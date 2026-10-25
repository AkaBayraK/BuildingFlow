# FAZ 02 — Şema ve migration

## Çalıştırma talimatı
MASTER_PROMPT.md ve AGENTS.md kurallarını uygula. Yalnızca bu fazı çalıştır. Başlamadan önce önceki faz çıktısını doğrula; yoksa dur ve bildir. FAZ 00 için önceki faz aranmaz.

## İşler
Mevcut veritabanına uyumlu additive migration üret. Tenant/management_area FK, benzersizlik, optimistic concurrency, indeksler, soft-delete, audit, idempotency anahtarları, transaction ve allocation bütünlüğü. Finansal tutarlar decimal, para birimi açık; immutable ledger/audit yaklaşımı. Rollback ve test verisi hazırla.

## Teslimatlar
migration, entity mapping, repository testleri.

## Kabul ve durma kuralı
- Mevcut kod korunur; ilgili testler çalışır veya neden çalışmadığı belgelenir.
- Tenant/management-area izolasyonu, güvenlik, lisans ve veri bütünlüğü kontrol edilir.
- Türkçe konsolda adım adım ilerleme, değişen dosyalar, testler, riskler ve geri alma planı yazılır.
- Faz bitince **DUR, kullanıcı onayını bekle**. Sonraki faza geçme.
