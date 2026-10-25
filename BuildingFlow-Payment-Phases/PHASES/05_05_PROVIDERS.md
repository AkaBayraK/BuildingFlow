# FAZ 05 — Ödeme sağlayıcı adaptörleri

## Çalıştırma talimatı
MASTER_PROMPT.md ve AGENTS.md kurallarını uygula. Yalnızca bu fazı çalıştır. Başlamadan önce önceki faz çıktısını doğrula; yoksa dur ve bildir. FAZ 00 için önceki faz aranmaz.

## İşler
Provider-neutral IPaymentProvider arayüzü ve sandbox mock adaptörü kur. iyzico/PayTR/banka POS için canlı entegrasyon kodlamadan önce resmi dokümantasyon, sözleşme, lisans, 3DS, webhook ve iade kabiliyetlerini doğrula; seçilen sağlayıcı için sandbox adapter uygula. Secret ve TLS kontrolleri.

## Teslimatlar
adaptör, sandbox contract testleri, entegrasyon matrisi.

## Kabul ve durma kuralı
- Mevcut kod korunur; ilgili testler çalışır veya neden çalışmadığı belgelenir.
- Tenant/management-area izolasyonu, güvenlik, lisans ve veri bütünlüğü kontrol edilir.
- Türkçe konsolda adım adım ilerleme, değişen dosyalar, testler, riskler ve geri alma planı yazılır.
- Faz bitince **DUR, kullanıcı onayını bekle**. Sonraki faza geçme.
