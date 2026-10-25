# FAZ 04 — Banka ve IBAN

## Çalıştırma talimatı
MASTER_PROMPT.md ve AGENTS.md kurallarını uygula. Yalnızca bu fazı çalıştır. Başlamadan önce önceki faz çıktısını doğrula; yoksa dur ve bildir. FAZ 00 için önceki faz aranmaz.

## İşler
Yönetim alanı bazında çoklu hesap, IBAN checksum, banka/hesap sahibi, para birimi, varsayılan hesap, geçerlilik, aktif/pasif, tahsilat türü ve isteğe bağlı blok yönlendirme uygula. IBAN değişiminde audit, iki aşamalı onay seçeneği, eski işlemlerin tarihsel hesap bilgisini koruma.

## Teslimatlar
banka hesap CRUD, onay akışı, tenant testleri.

## Kabul ve durma kuralı
- Mevcut kod korunur; ilgili testler çalışır veya neden çalışmadığı belgelenir.
- Tenant/management-area izolasyonu, güvenlik, lisans ve veri bütünlüğü kontrol edilir.
- Türkçe konsolda adım adım ilerleme, değişen dosyalar, testler, riskler ve geri alma planı yazılır.
- Faz bitince **DUR, kullanıcı onayını bekle**. Sonraki faza geçme.
