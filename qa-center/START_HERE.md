# Başlangıç
1. Bu dosyaları BuildingFlow repo kökünde `qa-center/` altına yerleştir (AkaAI entegrasyonu için ayrı repo erişimi gerekir).
2. Claude Code veya Codex'e aşağıdaki komutu ver.
3. **İlk çalıştırmada sadece FAZ 00 analizi yapılır.** GitHub issue oluşturma ve kodlama dahil hiçbir mutasyon kullanıcı onayı öncesi yapılmaz.

```text
qa-center/MASTER_PROMPT.md, qa-center/AGENTS.md, qa-center/PHASES.md,
qa-center/UI_SPEC.md ve qa-center/TEST_MATRIX.md dosyalarını oku.
BuildingFlow ve AkaAI projelerini salt okunur tara.
Yalnızca FAZ 00 analizini gerçekleştir; mevcut ekran/API/mobil/finans akışları,
testler, GitHub ve lisans durumunu doğrula. Türkçe konsol ve analiz raporu üret.
Hiçbir kodu, veritabanını, GitHub issue'sunu veya CI ayarını değiştirme.
Rapor sonunda dur ve onayımı bekle.
```

Not: GitHub task açma/kapama, AI entegrasyonu ve test otomasyonu ancak ilgili araç bağlantıları, erişim izinleri, test ortamı ve kullanıcı onayı sağlandığında gerçekleştirilebilir.
