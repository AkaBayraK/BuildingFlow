---
name: akadev-contract
description: API veya mimari değişiklikte etkilenen backend ve frontend tüketicilerinin sözleşmesini belirler.
---

# Mimari ve API sözleşmesi

Kök/ilgili modül AGENTS.md ve docs/BUILDINGFLOW_PROJECT_CORE.md dosyalarını oku. Frontend etkisinde docs/frontend-rules.md de okunur. Sürüm ve komutları manifestten doğrula.
İlgili docs/api belgesini ve gerçek endpoint/DTO tüketicilerini bul. Değişen path, alan, null davranışı, hata kodu, pagination, rol/aktif üyelik ve Property kapsamını tanımla.
Domain → Application → Infrastructure/API ayrımını ve modular monolith mimarisini koru. Yeni servis, bağımlılık veya domain kapsamı varsayma.
Her tüketici için gereken değişiklik, geriye uyumluluk ve kabul kanıtını belirle. Mutation için version/409, UUID/fingerprint replay ve belirsiz cevap davranışını ilgili domain sözleşmesinden al.
Persistence değişiminde docs/MIGRATIONS.md üzerinden snapshot, migration ve yükseltme/rollback etkisini incele; uygulanmış migration yeniden yazılmaz.
Uygulama yetkisi varsa ilgili üretici/tüketicileri birlikte tamamla; yalnız analiz isteniyorsa kod yazma. Ayrı Agent başlatma: bu depoda kullanıcı paralel çalışma istemedikçe tek Agent kullanılır.
