---
name: akadev-phase
description: Kullanıcının fazlara böldüğü BuildingFlow işinde doğru plan kimliği ve kabul kanıtlarını izler.
---

# Faz yönetimi

Yalnız açık faz planı veya fazlı görevde kullan; sıradan düzeltmeye yeni onay akışı ekleme. Geçerli kullanıcı yetkisini koru; önceden onaylı işlemi tekrar sorma.
İŞ, UI, ilk TEKNİK ve AkaDev AI planları ayrıdır. İŞ kaynağı milestones.ts, UI kaynağı frontend/buildingflow-ui-plan/PHASES.md, ilk TEKNİK kaynak core belgesidir. AkaDev AI kimlikleri bu sohbetin onaylı planından alınır.
Aktif fazı, teslimleri ve resmi adımları gerçek kaynaktan belirle; tarihsel prompt tamamlanmış işi yeniden başlatmaz. Toplam bilinmiyorsa BİLGİ MEVCUT DEĞİL yaz.
Başlangıç ve anlamlı aşamalarda kök AGENTS.md ilerleme biçimini uygula. Yüzde yalnız tamamlanan/resmi toplam üzerinden hesaplanır; test alt senaryoları paydayı şişirmez.
Yetkili geliştirme/protokol işinde mevcut TESTING, STATUS, SESSION ve CHANGELOG kaynaklarını güncelle. Salt okunur audit'te bu kayıtlar okunur fakat yazılmaz.
Sırf belge/Skill kuruldu diye ürün roadmap görevini tikleme. Aktif fazı kabul kanıtıyla tamamla; yeni faz kullanıcı onayı olmadan uygulanmaz.
Teslimde GEÇTİ / BAŞARISIZ / DOĞRULANAMADI ayrımını ve kalan gerçek işleri belirt; eksik runtime doğrulamasını kurulum başarısıyla karıştırma.
