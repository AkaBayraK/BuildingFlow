---
name: akadev-audit
description: İstenen salt okunur depo veya AI yetenek denetimini kapsam ve dosya kanıtlarıyla yürütür.
---

# Salt okunur denetim

Kök ve ilgili alt AGENTS.md dosyalarını oku. Kullanıcının seçtiği proje, görev ve faz sınırlarını belirle; tarihsel başlangıç/UI promptlarını yeni görev kabul etme.
Başlangıç ve bitişte `git status --short` karşılaştır. Önceden var olan farkları başlangıç durumu olarak kaydet; beklenmeyen farkı bildir ve dur.
Yalnız güvenli yerel metin ve dosya envanterini incele. Build, test, restore/install, scanner, ağ çağrısı, servis başlatma, migration veya veri yazma yapma.
Bu modda ilerleme, test, Skill/Agent kayıtları dahil hiçbir dosyayı güncelleme. Kullanıcı ayrıca rapor dosyası istediyse yalnız izinli raporu yaz; var olan raporu ezme.
Gizli dosya, credentials, dump ve gerçek kişisel veri içeriğini okuma. Kaynakta hassas değer fark edilirse değeri aktarma; yalnız güvenli konum/risk bildir.
node_modules, .git, bin/obj, build/dist/.next, coverage, vendor, artifacts ve generated çıktıları dışla. Repo dışı symlink takip etme; repo içi Skill kaynak bağlantısı okunabilir.
Her bulguya önem, güven, dosya/satır, etki ve öneri ekle. Bulunamayanı tespit edilemedi; çalıştırılmayanı ÇALIŞTIRILMADI yaz. Dosya varlığını runtime başarısı sayma.
