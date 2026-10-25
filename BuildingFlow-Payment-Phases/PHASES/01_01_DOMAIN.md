# FAZ 01 — Domain ve ADR

## Çalıştırma talimatı
MASTER_PROMPT.md ve AGENTS.md kurallarını uygula. Yalnızca bu fazı çalıştır. Başlamadan önce önceki faz çıktısını doğrula; yoksa dur ve bildir. FAZ 00 için önceki faz aranmaz.

## İşler
PaymentMethod, BankAccount, ProviderConfiguration, MerchantBinding, PaymentIntent, PaymentTransaction, Allocation, Receipt, Refund, Reconciliation gibi kavramları mevcut modellerle çakışmadan tasarla. Yönetim alanı/opsiyonel blok yönlendirme, dönemsel kişi-daire ve borç sorumlusu, kısmi ödeme ve komisyon politikasını tanımla. ADR ve durum geçişlerini yaz.

## Teslimatlar
domain ADR, durum diyagramı, yetki matrisi, kabul kriterleri.

## Kabul ve durma kuralı
- Mevcut kod korunur; ilgili testler çalışır veya neden çalışmadığı belgelenir.
- Tenant/management-area izolasyonu, güvenlik, lisans ve veri bütünlüğü kontrol edilir.
- Türkçe konsolda adım adım ilerleme, değişen dosyalar, testler, riskler ve geri alma planı yazılır.
- Faz bitince **DUR, kullanıcı onayını bekle**. Sonraki faza geçme.
