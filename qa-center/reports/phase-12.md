# [QA FAZ 12 | KAYIT 13/14] Hata analizi, GitHub otomasyonu ve kalite kapısı — GitHub #108

```text
[BUILDINGFLOW]
[FAZ QA 12 | KAYIT 13/14] Kalite kapısı
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR
[İLERLEME] FAZ 12 PHASES maddeleri 4/5 = %80,00 (performans kapısı kanıt bekliyor)
```

## Ne yapıldı
- `scripts/qa_github.py` (yeni paket yok, `gh` CLI):
  - `gate <report.json>`: regresyon, güvenlik, finans, performans, SKIPPED/BLOCKED kategorileri. FAIL → **RED**; kanıtı olmayan veya SKIPPED/BLOCKED kalemi olan kategori → **INCOMPLETE** (asla PASS değil). Çıkış 0/1/2.
  - `bugs <report.json> [--apply]`: issue bağlantısı olmayan her FAIL için tekrar üretim, beklenen/gerçek, kanıt ve severity (güvenlik/finans high, a11y low) içeren `[QA-BUG][parmak izi]` issue; mevcut parmak izleri aranır, mükerrer açılmaz.
  - `close-phase <report.json> <issue> [--apply]`: kapı sonucunu Türkçe yorumlar; **yalnız PASS ise kapatır**.
  - Varsayılan **dry-run**: `--apply` olmadan GitHub'a yazılmaz.
- `.github/workflows/qa-gate.yml`: **yalnız elle tetiklenir** (`workflow_dispatch`); izole CI testleri → kanıt raporu → kapı; GitHub'a yazma izni yok (`contents: read`), rapor job özetine yazılır.
- `test_qa_github.py` 15/15 (gh yerine kayıtçı; dry-run ağ çağrısı yapmaz, mükerrer/bağlı issue atlanır, RED kapanmaz, PASS kapanır, "Upload" performans sayılmaz, SKIPPED güvenlik INCOMPLETE). CI'a bağlandı.

## Gerçek rapor üzerinde (konsolide, 1082 kalem)
```text
[QA KAPI] RED
  regression: RED (2 FAIL)                → #87
  security: INCOMPLETE (1/150 SKIPPED)    → /consents/records/[recordId] örnek kayıt ister
  finance: PASS (176 kalem PASS)
  performance: INCOMPLETE (kanıt yok)
  skipped_blocked: INCOMPLETE (12 SKIPPED/BLOCKED)
```
`bugs` dry-run: 0 yeni bug (tek FAIL #87'ye bağlı). `close-phase … 107` dry-run: yorum hazırlandı, kapatma yok (RED).
Bir hata da bulundu ve düzeltildi: ilk sürüm "Load" geçen test adlarını (Upload) performans kanıtı sayıp yanlış PASS veriyordu.

## PHASES kontrol listesi
| Madde | Durum |
|---|---|
| Kanıtlı bug issue, tekrar üretim, severity | PASS (dry-run; gerçek açma `--apply` ile) |
| Faz issue checklist + Türkçe güncelleme | PASS (faz issue'ları #81–#107) |
| Tüm zorunlu PASS ise otomatik kapama, değilse açık | PASS |
| Güvenlik, regresyon, performans, mutabakat kapısı | KISMİ: performans testi yok → INCOMPLETE; mutabakat finans kategorisinde |
| CI entegrasyonu, varsayılan manuel | PASS (workflow GitHub'da çalıştırılmadı; push sizde) |

[TOKEN] BİLGİ MEVCUT DEĞİL.
