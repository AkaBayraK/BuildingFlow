# Güvenlik ve Gizlilik — Zorunlu
- OWASP ASVS/API Security ve tehdit modelini ilgili değişiklik için uygula; istemci doğrulamasını sunucu yetkisi sayma.
- Tenant izolasyonunu sunucuda her okuma/yazma/export/cache anahtarında zorunlu kıl; IDOR/BOLA negatif testleri ekle.
- En az ayrıcalık, deny-by-default, kısa ömürlü token, güvenli session, rate limit ve audit.
- Secret, token, kişisel veri ve ham hassas değerleri kod, URL, log, trace, exception, analitik ve test fixture'ına koyma.
- Input validation, output encoding, injection/XSS/CSRF/SSRF, dosya yükleme ve webhook imza kontrollerini uygun yerde uygula.
- Şifreleme anahtarı yönetimi, veri saklama/silme ve KVKK süreçlerini göz önünde bulundur; uyumluluk garantisi iddia etme.
- Yetki/politika servisi arızasında hassas veriye erişim fail-closed olsun; kesinti etkisini ve geri dönüşü açıkla.
- Migration ve kritik işlem öncesi geri dönüş planı oluştur; gerçek veriyi veya ortamı onaysız silme.
