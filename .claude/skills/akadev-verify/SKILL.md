---
name: akadev-verify
description: Yetkili değişikliğin kabul kriterlerini BuildingFlow mevcut kalite prosedürleriyle kanıtlar.
---

# Kanıta dayalı doğrulama

Kök/ilgili modül AGENTS.md, docs/TESTING.md ve değişen alanın manifest/CI komutlarını oku. Önce anlamlı kabul senaryolarını belirle; kullanıcı salt okunur audit istediyse komut çalıştırma.
Frontend davranışında mevcut npm test/typecheck/build komutlarını kullan. Lint komutu manifestte yoksa varmış gibi çalıştırma. Sadece belge/Skill değişiminde metadata, bağlantı, kapsam ve kaynak koruma kontrolleri yeterlidir.
Backend davranışında scripts/test.sh ve ilgili .NET test projesinin kapsamını incele. Persistence işinde docs/MIGRATIONS.md; yayın/yedek işinde ilgili RELEASE/BACKUP_RESTORE rehberi kullanılır.
scripts/ci.sh temizleme, restore/install, çıktı ve izole servis/DB işlemleri yapabilir; check-licenses.py envanter yazar. Bunları salt okunur araç sayma; çalıştırmadan etkilerini ve kullanıcı görev yetkisini değerlendir.
Gerçek testler yalnız belgelenmiş ayrı PostgreSQL ve fixture kaynaklarını kullanır. Mevcut servis/DB, kullanıcı kayıtları ve kimlik bilgileri değiştirilmez. Ortam engelini gerçek izin mekanizmasıyla bildir.
Derleme davranış kanıtı değildir. Değişen endpoint'te güvenli pozitif/negatif rol/üyelik/version yollarını, UI'da gerçek etkileşim/odak/error/locale durumunu doğrula. Ortam/cihaz yoksa DOĞRULANAMADI yaz.
Çalıştırılan sonucu bu turun kanıtıyla raporla; geçmiş TESTING sonucu yeniden çalışmış sayılmaz. Test sayısını kapsam yüzdesi veya üretime hazırlık oranı diye sunma.
