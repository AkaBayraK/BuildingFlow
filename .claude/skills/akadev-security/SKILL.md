---
name: akadev-security
description: İstenen güvenlik incelemesinde veya auth, üyelik, hassas veri yolları değişiminde sınırları kontrol eder.
---

# Güvenlik incelemesi

Kök/ilgili modül AGENTS.md ve ilgili docs/api sözleşmesini oku. Değişen kimlik, Property üyeliği, veri scope'u ve girdi/çıktı sınırlarını belirle.
Liste, detay, mutation, geçmiş ve belge yollarında rol + aktif üyelik + kaynak kapsamını birlikte değerlendir. IgnoreQueryFilters kullanımında scope'un açıkça korunduğunu incele; UI AuthGate'i backend yetkisi sayma.
Kimlik işinde PKCE ve negatif JWT/yetki senaryolarını koru; password grant açma. Token/reset linki/PII'yi log, anonim SSR, cache veya NEXT_PUBLIC yapılandırmasına taşıma.
Güvenli hata katalogları, query/SQL/URL/dosya sink'leri, MIME/imza ve kontrollü indirme davranışlarını yalnız değişen kapsamda incele. Telemetride kişisel ve yüksek kardinaliteli etiketleri kontrol et.
MCP gerekirse araç adı, erişim alanı ve yazma etkisini açıkla; araç yoksa başka güçlü erişimi otomatik yetkili sayma. Bu inceleme MCP veya scanner kurulumu sağlamaz.
Bulguya dosya/satır, güven, önem, etki ve dar çözüm ekle. Hipotezi doğrulanmış açık sayma; test/scanner çalıştırılmadıysa bunu açıkça bildir.
İnceleme kod düzeltme yetkisi değildir; düzeltme yalnız kullanıcının yetkilendirdiği görev kapsamındaysa yapılır.
