# BF-VERIFY — Başlangıç ve çalıştırma

Bu paket **8 fazdır (00–07)**. Kanonik faz/kabul kaynağı [PHASES.md](PHASES.md), kurallar [MASTER_PROMPT.md](MASTER_PROMPT.md).
FAZ00/01 tamamlandı. Kullanıcı FAZ02/#45'i başlattı; FAZ03–07 başlatılmadı.

1. FAZ 00 analizi: [00-analysis.md](../docs/refactor/contact-verification/00-analysis.md).
2. Görev numaraları ve @BuildingFlow pano bağlantısı PHASES.md'dedir. Her faz bir GitHub issue; yeni Codex sohbeti veya ikinci backlog açılmaz.
3. Sonraki faz kullanıcı tarafından başlatıldığında şu promptu kullan:

```text
buildingflow-contact-verification-prompts/MASTER_PROMPT.md ve PHASES.md dosyalarını oku.
Yalnız BF-VERIFY FAZ 01'i, phases/PHASE_01.md uyarınca uygula.
Önce ilgili GitHub görevini ve gerçek önkoşulları incele; mevcut değişiklikleri koru.
Türkçe konsol ilerlemesi ver. Kapsam ve zorunlu testler tamamlanınca görevde
Yapılanlar bölümünü dosya/komut/kanıtla ayrıntılı maddeler halinde güncelle.
Eksik madde veya zorunlu kanıt varsa görevi açık bırak ve nedeni yaz.
Hiçbir eksik yoksa taskı completed olarak kapat, panoyu Done yap ve ikisini yeniden oku.
Sonraki faz ayrıca başlatılmadıysa dur. Canlı ayar/DB/servis ve kullanıcı verisine dokunma.
```

Diğer fazlarda 01 değerini onaylanan NN ile değiştir; birden fazla faz açıkça onaylandıysa aynı onayı tekrar sorma.
Teslim biçimi: [TASK_REPORT_TEMPLATE.md](TASK_REPORT_TEMPLATE.md).
Kabul senaryoları: [ACCEPTANCE_MATRIX.md](../docs/refactor/contact-verification/ACCEPTANCE_MATRIX.md).



## Güncel kullanıcı yetkisi — 22.10.2026

Prompt/görev/analiz teslimi tamamlandı; #43 Closed(completed), Project3 Done son okumayla doğrulandı. Kullanıcının **ilk fazdan başlayabilirsin** mesajı BF-VERIFY FAZ01/#44 ilk uygulama fazını başlattı; FAZ02–07 henüz yetkili değil. Yukarıdaki önce-yalnız-FAZ00 ifadeleri başlangıç kapsamının tarihçesidir; güncel kapsam bu kayıttır.


## Son durum — 22.10.2026

FAZ00/#43 ve ilk uygulama FAZ01/#44 tamamlandı;iki issue Closed(completed),Project3 Done,0 açık maddeyle son okumada doğrulandı.Kalan #45–#50 açık/Backlog;FAZ02 ayrıca başlatılmadı.FAZ01 kanıtı:14/14 PostgreSQL/API+6/6 domain ve build GEÇTİ.Gerçek e-posta/SMS gönderim workflow'u ve istemci doğrulama ekranları sonraki fazlardadır.


## Güncel kapsam — BF-VERIFY 02 / 22.10.2026

Kullanıcı sıradaki fazı başlattı; #45 In progress. D1/D2 açık onayı: yeni hesaplarda e-posta doğrulaması zorunlu, telefon isteğe bağlı; mevcut doğrulanmış adresler korunur, eski hesaplar kontrollü geçirilir, e-postaya erişim kaybında otomatik VERIFIED verilmeden yetkili destek gerekir. Canlı zorunluluk bu fazda açılmaz; EmailEnabled opt-in olarak kapalı kalır. FAZ03–07 başlatılmadı.


## Son doğrulanan durum — BF-VERIFY02

FAZ00/01/02 tamamlandı; #45 Closed(completed), Project3 Done,12/12 madde son uzak okumada doğrulandı. Faz3/3=%100,00;plan3/8=%37,50. FAZ03–07 başlatılmadı. Kanıt: `docs/refactor/contact-verification/02-report.md`. Önceki kapsam paragrafları tarihsel kayıttır.
