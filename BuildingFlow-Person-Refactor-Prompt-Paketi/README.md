# BuildingFlow — Person Refactor Faz Paketi

1. Bu klasörün içeriğini BuildingFlow depo köküne **mevcut aynı adlı dosyaları körlemesine ezmeden** kopyalayın. Mevcut `AGENTS.md` veya başka talimatlar varsa önce birleştirin.
2. Codex/Claude Code'a `MASTER_PROMPT.md` ve `phases/PHASE-01.md` dosyalarını okutun.
3. Faz 01 yalnızca analiz yapar. Raporu kontrol edip onayladıktan sonra sıradaki fazı ayrı komutla başlatın.
4. Kodun gerçek durumu bilinmediğinden faz 02+ tasarımları repo bulgularına göre uyarlanmalıdır.

**İlk çalıştırma promptu:**

> `MASTER_PROMPT.md`, `STANDARDS.md`, `PHASES.md`, `AGENTS.md` ve `phases/PHASE-01.md` dosyalarını oku. Mevcut BuildingFlow kodunu önce analiz et. Sadece FAZ 01'i çalıştır. Kod, veritabanı, migration ve bağımlılıkları değiştirme. Adımları Türkçe konsola yaz; analiz raporunu oluştur, riskleri açıkla ve onayımı bekle.
