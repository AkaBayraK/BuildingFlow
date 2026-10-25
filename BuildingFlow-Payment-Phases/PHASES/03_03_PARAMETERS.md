# FAZ 03 — Parametre API ve yetki

## Çalıştırma talimatı
MASTER_PROMPT.md ve AGENTS.md kurallarını uygula. Yalnızca bu fazı çalıştır. Başlamadan önce önceki faz çıktısını doğrula; yoksa dur ve bildir. FAZ 00 için önceki faz aranmaz.

## İşler
SystemAdmin sağlayıcı kataloğu, yönetim alanı ödeme yöntemleri/komisyon/kısmi ödeme/makbuz parametreleri için API ve doğrulama geliştir. Keycloak rol ve resource bazlı tenant izolasyonu; deny-by-default, DTO ve validation. Gizli anahtarlar vault/secret referansı; UI/API yanıtında sırları gösterme.

## Teslimatlar
API endpointleri, yetki testleri, OpenAPI.

## Kabul ve durma kuralı
- Mevcut kod korunur; ilgili testler çalışır veya neden çalışmadığı belgelenir.
- Tenant/management-area izolasyonu, güvenlik, lisans ve veri bütünlüğü kontrol edilir.
- Türkçe konsolda adım adım ilerleme, değişen dosyalar, testler, riskler ve geri alma planı yazılır.
- Faz bitince **DUR, kullanıcı onayını bekle**. Sonraki faza geçme.
