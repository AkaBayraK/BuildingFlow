# Kullanım — Codex / Claude Code
1. Bu üç dosyayı BuildingFlow repository köküne (veya docs/prompts altına) koy.
2. Önce aşağıdaki komutu ajan sohbetine gönder:

`MASTER_PROMPT.md ve PHASES.md dosyalarını oku. Sadece FAZ 00'ı uygula. Önce mevcut BuildingFlow backend, merkez web, malik/kiracı web portalı ve mobil kodlarını incele. Hiçbir uygulama kodunu, migration'ı veya konfigürasyonu değiştirme. docs/refactor/multi-property/00-analysis.md raporunu oluştur. Türkçe konsol ilerlemesi göster, test/etki/migration risklerini özetle ve onayımı bekle.`

3. Onaydan sonra: `MASTER_PROMPT.md ve PHASES.md kurallarına göre yalnız FAZ 01'i uygula; ilgili testleri çalıştır; raporla ve dur.`
4. Sonraki fazları tek tek ilerlet. Gerçek DB migration ve production deployment için ayrıca onay ver.
