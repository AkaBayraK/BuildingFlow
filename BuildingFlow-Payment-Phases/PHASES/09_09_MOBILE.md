# FAZ 09 — Mobil ödeme ve offline

## Çalıştırma talimatı
MASTER_PROMPT.md ve AGENTS.md kurallarını uygula. Yalnızca bu fazı çalıştır. Başlamadan önce önceki faz çıktısını doğrula; yoksa dur ve bildir. FAZ 00 için önceki faz aranmaz.

## İşler
Mevcut mobil teknoloji ve SQLite senkronizasyonuna uy. Daire seçimi, dönemsel borçlar, ödeme yöntemi, güvenli hosted checkout, sonuç bekleniyor/başarılı/başarısız, makbuz/geçmiş. Offline: sadece önbellek ve son senkronizasyon bilgisi; ödeme başlatma, ödeme tekrarı, ödeme kuyruğu yok. Çoklu daire ve roller için erişim kontrolü.

## Teslimatlar
mobil akışlar, offline/online testleri.

## Kabul ve durma kuralı
- Mevcut kod korunur; ilgili testler çalışır veya neden çalışmadığı belgelenir.
- Tenant/management-area izolasyonu, güvenlik, lisans ve veri bütünlüğü kontrol edilir.
- Türkçe konsolda adım adım ilerleme, değişen dosyalar, testler, riskler ve geri alma planı yazılır.
- Faz bitince **DUR, kullanıcı onayını bekle**. Sonraki faza geçme.
