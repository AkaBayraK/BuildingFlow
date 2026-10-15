# Performans ve Verimlilik
- Ölçmeden performans artışı iddia etme. Önce darboğazı tanımla, gerekirse profil/benchmark ekle.
- DB sorgularında N+1, gereksiz join, full scan, sınırsız sonuç, indeks ve transaction kapsamını kontrol et.
- Büyük listelerde server-side pagination/filter/sort; ağda gereksiz payload ve tekrar istekten kaçın.
- Cache ancak TTL, invalidation, tenant ayrımı ve hassas veri güvenliği tanımlanırsa kullan.
- Kuyruk, retry, timeout, circuit breaker ve idempotency davranışlarını dış sistem sınırında tasarla.
- UI'da gereksiz render, bundle büyüklüğü ve ağır bağımlılıklardan kaçın; erişilebilirliği koru.
- Kod kalitesi/güvenlik/testi hız için feda etme.
