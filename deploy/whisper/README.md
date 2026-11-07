# Yerel ses tanıma (whisper.cpp) — AI Asistan sesli yazışma

İsteğe bağlı `speech` Compose profili. Varsayılan kurulumda **çalışmaz**; açılmazsa AI Asistan'daki "Sesli yazışma" anahtarı "devre dışı — hizmet çalışmıyor" gösterir, asistanın geri kalanı etkilenmez.

- **İmaj** `buildingflow-whisper:local`: `Dockerfile`, ggml-org/whisper.cpp **v1.9.5** resmi kaynak arşivini indirir, SHA256 `ff1a9053feb509ff9d7729703355541ae9690073a6b1c40eb692c962e0dc1720` ile doğrular ve yalnız `whisper-server` ikilisini derler. Upstream MIT lisansı imajda `/usr/share/licenses/whisper.cpp/LICENSE`. Kullanıcı `whisper` (uid 10001), salt okunur kök dosya sistemi, `no-new-privileges`.
- **Model** imaja gömülmez. `whisper-model` tek seferlik işi `fetch-model.sh` ile `WHISPER_MODEL`'i Hugging Face `ggerganov/whisper.cpp` deposundan indirir; `WHISPER_MODEL_SHA256` tutmazsa siler ve başarısız olur. Model `whisper-models` volume'unda kalır, sonraki açılışlarda yalnız doğrulanır.
  Varsayılan `ggml-large-v3-turbo-q5_0.bin` (574 MB, SHA256 `394221709cd5ad1f40c46e6031ca61bce88931e6e088c188294c6d5a55ffa7e2`). Daha hafif: `ggml-small.bin` (488 MB, `1be3a9b2063867b937e64e2ec7483364a79917e157fa98c5d94b5c1fffea987b`). İkisi birlikte değiştirilir.
- **Ağ**: API'ye `http://whisper:8080/inference` (`Assistant__Speech__BaseUrl/Path` compose'da otomatik). Yerel geliştirme için yalnız `127.0.0.1:${WHISPER_PORT:-8178}` yayınlanır; üretimde bu port satırı kaldırılır.
- **Kaynak**: `WHISPER_MEMORY_LIMIT` (3g), `WHISPER_CPUS` (4), `WHISPER_THREADS` (4, whisper-server `-t`; `WHISPER_CPUS` ile eşit tutun). Healthcheck kök sayfa.
- **Gizlilik**: API sesi bellekte tamponlar (diske taşmaz), sunucuya bir kez gönderir; whisper.cpp `--convert` kullanılmadığından geçici dosya yazmaz. Tarayıcı kaydı cihazda 16 kHz mono WAV'a çevrilir.
- **Yedek**: `whisper-models` volume'u yedek kapsamında **gerekmez**; yeniden indirilebilir, kişisel veri içermez.

```bash
bash scripts/dev.sh speech        # imajı derler, modeli doğrular, servisi sağlıklı başlatır
bash scripts/dev.sh speech-stop   # durdurur (volume kalır)
docker volume rm buildingflow_whisper-models   # modeli tamamen silmek için (yeniden indirilir)
```

## Performans (07.11.2026 ölçümü)

Ölçüm: Apple M5 Pro, Docker Desktop VM 18 CPU / 7,7 GiB (aarch64), yalnız CPU — macOS'taki Docker GPU'ya (Metal)
erişemez. 5 Türkçe cümle (~2–3 sn ses), `/inference` uçtan uca. Ayrıntı ve doğruluk notları `docs/TESTING.md`.

| Model | `--cpus` / `-t` | İstek başına | Tepe bellek | Birebir doğru |
|---|---|---|---|---|
| large-v3-turbo-q5_0 (varsayılan) | 4 / 4 | ~35,5 sn | ~730 MiB | 4/5 |
| large-v3-turbo-q5_0 | 18 / 16 | ~10,6 sn | ~740 MiB | 4/5 |
| ggml-small | 4 / 4 | ~3,2 sn | ~650 MiB | 2/5 |
| ggml-small | 18 / 16 | ~1,7 sn | ~640 MiB | 2/5 |

Aynı Mac'te Homebrew `whisper-server` (Metal) 0,24–1,2 sn; Mac geliştirmede onu kullanmak daha hızlıdır.

**Başka sunucuda ne beklenir (yorum, ölçüm değil):**
- Süre CPU zamanıyla ölçeklenir: large ≈ 140–170 çekirdek-saniye, small ≈ 13–25 çekirdek-saniye / istek.
  Thread'i 4'ten 16'ya çıkarmak large'da ~3,4×, small'da ~2× hızlandırdı. Modern x86 (AVX2/AVX-512) sunucu
  çekirdeği bu Mac çekirdeğine yakın ya da biraz yavaştır; kabaca large için 4 çekirdekte 35–50 sn,
  8 çekirdekte ~18–25 sn, 16 çekirdekte ~10–15 sn beklenir. Gerçek değer sunucuda ölçülmelidir.
- Whisper sesi 30 sn'lik pencerelerle işler: 30 sn'ye kadar kısa kayıt yaklaşık aynı süreyi, 60 sn'lik kayıt
  yaklaşık iki katını alır. 4 çekirdekte large ile uzun kayıt `Assistant:Speech:TimeoutSeconds` (60) sınırını
  aşabilir.
- Bellek: model + çalışma ~0,7 GiB; `WHISPER_MEMORY_LIMIT=3g` rahat yeterlidir.
- Thread: compose `-t ${WHISPER_THREADS:-4}` geçirir. `WHISPER_CPUS`'u artırırken `WHISPER_THREADS`'i de aynı
  değere çekin; yalnız CPU sınırını artırmak hızlandırmaz (whisper-server varsayılanı 4 thread).
- Öneri: doğruluk için varsayılan large-v3-turbo-q5_0 kalsın; CPU-only sunucuda ≥8–16 çekirdek ve eşleşen
  thread ayrılsın veya GPU'lu sunucu kullanılsın. ggml-small hızlı ama finans terimlerini ("aidat", "bütçe")
  bozduğu için yalnız çok kısıtlı donanımda tercih edilmeli. Varsayılan değiştirilmedi.
