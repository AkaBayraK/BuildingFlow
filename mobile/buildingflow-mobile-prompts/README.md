# BuildingFlow Mobile — Fazlı Geliştirme Prompt Paketi

Bu paket, BuildingFlow mobil uygulamasını mevcut projeyi inceleyerek ve her seferinde yalnızca bir onaylı fazı uygulayarak geliştirmek için hazırlanmıştır.

## Dosyalar

- `MASTER_PROMPT.md` — her oturumda kullanılacak ana çalışma kuralları ve ilk başlatma promptu
- `AGENTS.md` — Codex/Claude Code için kalıcı proje talimatları
- `PHASES.md` — faz kapsamları, tamamlanma ölçütleri ve faz başlatma promptu şablonu

## Kullanım

1. Arşivi BuildingFlow deposunun kök dizinine açın.
2. Mevcut `AGENTS.md` varsa üzerine yazmayın; mobil talimatları mevcut dosyaya kontrollü biçimde ekleyin veya `mobile/AGENTS.md` oluşturun.
3. İlk olarak `MASTER_PROMPT.md` içindeki **İlk Çalıştırma Promptu**nu Codex/Claude Code'a verin.
4. İlk çalıştırma sadece Faz 01 analizini yapar. Kod değişikliği ve yeni dosya oluşturma yasaktır.
5. Raporu inceledikten sonra her faz için `PHASES.md` içindeki başlatma şablonunu kullanın. Her çağrıda yalnızca bir faz yürütülür.

## Mimari karar kapısı

Varsayılan öneri cihaz başına SQLite veritabanı, çevrimdışı kullanım ve bağlantı olduğunda isteğe bağlı API senkronizasyonudur. Mobil uygulama hiçbir koşulda veritabanına doğrudan bağlanmaz. Kullanıcının “merkez DB'den tamamen bağımsız” ifadesi tüm sistemin merkezi sunucusuz çalışması anlamına geliyorsa, bunun kapsamı Faz 01 raporunda ayrı seçenek olarak açıklanmalı ve senkronizasyon kodlanmadan önce karar beklenmelidir.

## Optimize edilmiş v2 paket
Orijinal fazlar ve varlıklar korunmuştur. Eklenen standartlar: `CODING_STANDARDS.md`, `LICENSE_POLICY.md`, `SECURITY_STANDARDS.md`, `PERFORMANCE_RULES.md`, `TOKEN_OPTIMIZATION.md`, `PHASE_EXECUTION_PROTOCOL.md`. Mevcut `AGENTS.md` dosyanız varsa **doğrudan üzerine yazmayın**; farklılıkları inceleyip kuralları birleştirin. Proje kodu bu pakette çalıştırılmamış ve lisanslar çevrimiçi doğrulanmamıştır.
