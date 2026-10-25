# FAZ 06 — Ödeme akışı

## Çalıştırma talimatı
MASTER_PROMPT.md ve AGENTS.md kurallarını uygula. Yalnızca bu fazı çalıştır. Başlamadan önce önceki faz çıktısını doğrula; yoksa dur ve bildir. FAZ 00 için önceki faz aranmaz.

## İşler
Sunucu tutarı/borcu yeniden hesaplayarak PaymentIntent oluşturur; idempotency, expiration, concurrency ve payment allocation reservation tasarla. Hosted checkout/3DS yönlendirmesi; webhook imza+timestamp+replay doğrulama, tekrarlı/sırasız bildirimler, provider status verification, atomik state transitions. İstemci dönüşünü başarı sayma.

## Teslimatlar
state machine, ödeme API, webhook testleri.

## Kabul ve durma kuralı
- Mevcut kod korunur; ilgili testler çalışır veya neden çalışmadığı belgelenir.
- Tenant/management-area izolasyonu, güvenlik, lisans ve veri bütünlüğü kontrol edilir.
- Türkçe konsolda adım adım ilerleme, değişen dosyalar, testler, riskler ve geri alma planı yazılır.
- Faz bitince **DUR, kullanıcı onayını bekle**. Sonraki faza geçme.
