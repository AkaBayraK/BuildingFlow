# BuildingFlow AkaDev ortak çekirdek

AI FAZ 03 yalnız BuildingFlow ortak prosedür kurulumudur; AkaAI denetimi henüz yoktur.
Bu yapı üç projeye kesinleşmiş evrensel politika olarak sunulmaz.

## Kaynak ve kullanım

Kural: kök ve ilgili modül AGENTS.md. Kanonik Skills `.claude/skills/`; Codex için
`.agents/skills/` aynı kaynaklara repo içi symlink içerir. Claude kök ve frontend
CLAUDE.md üzerinden ilgili AGENTS.md'yi import eder. Global ayar değiştirilmez.

Skill kataloğu ve tetikleme bağlamı [SKILLS_AND_AGENTS.md](SKILLS_AND_AGENTS.md) içindedir.
Codex açık seçim örneği `$akadev-audit`; Claude örneği `/akadev-audit`. Yeni Skills
göreve uygun açıklamalarıyla otomatik seçilebilir; seçim mutasyon izni değildir.

Audit kod, kayıt, ortam ve veri değiştirmez. Doğrulama komutları çıktı veya
fixture/servis işlemi yapabilir; etkiler ve kullanıcı kapsamı önce incelenir.
Lisans izinleri [licenses.md](licenses.md), mimari [core](BUILDINGFLOW_PROJECT_CORE.md),
frontend standardı [Convention](frontend-rules.md) kaynağından alınır.

## Faz sınırı

Tek Agent politikası korunmuştur; özel Agent, MCP, domain Skill veya CI paketi
kurulmamıştır. FAZ 04 persistence/finans/yerleşim prosedürlerinin ayrı kapsamıdır.
Gerçek Codex/Claude keşif, tetikleme ve davranış senaryoları FAZ 05'te doğrulanır;
statik dosya doğrulaması runtime başarısı değildir. Lint/SAST/kapsam ölçümü ayrı
uygulama kapsamı olmaya devam eder. Ürün ve UI roadmap işaretleri değiştirilmez.

Keşif referansı: https://learn.chatgpt.com/docs/build-skills
Kabul kanıtı: [TESTING.md](TESTING.md); ilerleme özeti mevcut progress kayıtlarında.
