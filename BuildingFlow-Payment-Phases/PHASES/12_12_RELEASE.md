# FAZ 12 — Canlı öncesi kontrol

## Çalıştırma talimatı
MASTER_PROMPT.md ve AGENTS.md kurallarını uygula. Yalnızca bu fazı çalıştır. Başlamadan önce önceki faz çıktısını doğrula; yoksa dur ve bildir. FAZ 00 için önceki faz aranmaz.

## İşler
Sandbox uçtan uca demo; sözleşme/uyum/PCI kapsamı, sağlayıcı hesabı, IBAN doğrulama, üretim secret yönetimi, rollback, monitoring, alarm, destek prosedürü ve pilot tenant checklist hazırla. Canlı anahtar yükleme, canlı tahsilat ve deploy yapma; ayrı açık onay iste.

## Teslimatlar
go/no-go kontrol listesi ve pilot planı.

## Kabul ve durma kuralı
- Mevcut kod korunur; ilgili testler çalışır veya neden çalışmadığı belgelenir.
- Tenant/management-area izolasyonu, güvenlik, lisans ve veri bütünlüğü kontrol edilir.
- Türkçe konsolda adım adım ilerleme, değişen dosyalar, testler, riskler ve geri alma planı yazılır.
- Faz bitince **DUR, kullanıcı onayını bekle**. Sonraki faza geçme.
