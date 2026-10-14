---
name: akadev-license
description: Bağımlılık, imaj veya harici kaynak değişiminde BuildingFlow lisans politikasını ve dağıtım envanterini değerlendirir.
---

# Lisans ve bağımlılık incelemesi

Kök AGENTS.md, docs/licenses.md ve ilgili manifest/lockfile farkını oku. İzin verilen seçenekleri bu projenin politikasından al; başka projenin yasak listesiyle değiştirme.
Tam sürüm, doğrudan/geçişli bağımlılık, lisans ifadesi, kaynak ve dağıtım artefaktını belirle. Yeni sürüm/lisans işinde resmi kaynağı doğrula; bilinmeyen lisansı uygun sayma.
Çift lisans için seçilen izinli seçeneği açıkça kaydet. AND/OR ifadelerini ve copyleft/NOTICE/yeniden bağlama yükümlülüklerini basit metadata atom kontrolüyle geçmiş sayma.
Container lisansı, container OS paketleri ve dağıtım/ticari yükümlülükler npm/NuGet kontrolünden ayrıdır. MinIO SDK ile MinIO sunucusunun lisansını aynı varsayma.
scripts/check-licenses.py metadata'yı kontrol eder ve docs/licenses/package-inventory.json yazar. Yetkili restore/npm ci sonrası mevcut politika gereği çalıştır; salt okunur audit'te çalıştırma.
Lisans/NOTICE dosyalarını koru; görev kapsamında yeni bağımlılık yoksa envanteri sırf bu Skill kullanıldı diye yeniden üretme.
Teslimde kaynak, seçilen seçenek, envanter sonucu ve doğrulanamayan dağıtım koşullarını belirt. Kontrol politikası veya bağımlılık değişimini kullanıcı kapsamı dışında yapma.
