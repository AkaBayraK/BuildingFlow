# FAZ 00 — Salt okunur keşif

## Çalıştırma talimatı
MASTER_PROMPT.md ve AGENTS.md kurallarını uygula. Yalnızca bu fazı çalıştır. Başlamadan önce önceki faz çıktısını doğrula; yoksa dur ve bildir. FAZ 00 için önceki faz aranmaz.

## İşler
Repo ağacı, .NET solution, Next.js, mobil, Keycloak, muhasebe/aidat, tahakkuk, kişi–daire–rol, tenant modeli, migration ve testleri hedefli incele. Mevcut veri akışını ve entegrasyon noktalarını belgeleyip eksikleri önceliklendir. Kod, bağımlılık, migration değiştirme.

## Teslimatlar
docs/payment/00-discovery.md; mevcut-yeni bileşen eşlemesi; riskler; faz 01 önerisi.

## Kabul ve durma kuralı
- Mevcut kod korunur; ilgili testler çalışır veya neden çalışmadığı belgelenir.
- Tenant/management-area izolasyonu, güvenlik, lisans ve veri bütünlüğü kontrol edilir.
- Türkçe konsolda adım adım ilerleme, değişen dosyalar, testler, riskler ve geri alma planı yazılır.
- Faz bitince **DUR, kullanıcı onayını bekle**. Sonraki faza geçme.
