# Faz Uygulama Protokolü
1. Faz kimliği, hedef, kabul ölçütü ve kullanıcı onayı doğrula; onay yoksa kodlama yapma.
2. `git status --short` ile kullanıcı değişikliklerini koru; mevcut faz özetini incele.
3. Yalnızca etkilenen modül, ortak servisler ve ilgili testlerde hedefli keşif yap.
4. Lisans, güvenlik, performans, API/DB geriye uyumluluk risklerini değerlendir; belirsiz kritik kararı kullanıcıya sor.
5. Küçük adımlarla minimal diff uygula; her adımda kısa Türkçe ilerleme bildir.
6. İlgili testler + gerekiyorsa kapsamlı test; yapılmayan testleri açıkça belirt.
7. Faz özeti: değişen dosyalar, test sonuçları, lisans değişimi, riskler, açık işler; sonraki faz için dur.
Özel durum: salt-okunur analiz fazında uygulama kodu, config, bağımlılık veya dokümantasyon dosyası yazma; mevcut fazın daha katı kuralları önceliklidir.
