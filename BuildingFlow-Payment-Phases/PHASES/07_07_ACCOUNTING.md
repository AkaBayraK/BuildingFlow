# FAZ 07 — Tahsilat ve muhasebe

## Çalıştırma talimatı
MASTER_PROMPT.md ve AGENTS.md kurallarını uygula. Yalnızca bu fazı çalıştır. Başlamadan önce önceki faz çıktısını doğrula; yoksa dur ve bildir. FAZ 00 için önceki faz aranmaz.

## İşler
Yalnızca doğrulanmış tahsilatta borç dağıtımı/kapatma ve muhasebe kayıtlarını atomik/uzlaştırılabilir şekilde üret. Kısmi ödeme, fazla ödeme, birden çok borca dağıtım, makbuz numarası ve denetim izi. İade/chargeback halinde kayıtları silme, ters kayıt oluştur. Mevcut aidat tahakkukunu bozma.

## Teslimatlar
tahsilat servisi, makbuz, muhasebe testleri.

## Kabul ve durma kuralı
- Mevcut kod korunur; ilgili testler çalışır veya neden çalışmadığı belgelenir.
- Tenant/management-area izolasyonu, güvenlik, lisans ve veri bütünlüğü kontrol edilir.
- Türkçe konsolda adım adım ilerleme, değişen dosyalar, testler, riskler ve geri alma planı yazılır.
- Faz bitince **DUR, kullanıcı onayını bekle**. Sonraki faza geçme.
