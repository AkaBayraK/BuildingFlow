# Yerel ses tanıma (whisper.cpp) — AI Asistan sesli yazışma

İsteğe bağlı `speech` Compose profili. Varsayılan kurulumda **çalışmaz**; açılmazsa AI Asistan'daki "Sesli yazışma" anahtarı "devre dışı — hizmet çalışmıyor" gösterir, asistanın geri kalanı etkilenmez.

- **İmaj** `buildingflow-whisper:local`: `Dockerfile`, ggml-org/whisper.cpp **v1.9.5** resmi kaynak arşivini indirir, SHA256 `ff1a9053feb509ff9d7729703355541ae9690073a6b1c40eb692c962e0dc1720` ile doğrular ve yalnız `whisper-server` ikilisini derler. Upstream MIT lisansı imajda `/usr/share/licenses/whisper.cpp/LICENSE`. Kullanıcı `whisper` (uid 10001), salt okunur kök dosya sistemi, `no-new-privileges`.
- **Model** imaja gömülmez. `whisper-model` tek seferlik işi `fetch-model.sh` ile `WHISPER_MODEL`'i Hugging Face `ggerganov/whisper.cpp` deposundan indirir; `WHISPER_MODEL_SHA256` tutmazsa siler ve başarısız olur. Model `whisper-models` volume'unda kalır, sonraki açılışlarda yalnız doğrulanır.
  Varsayılan `ggml-large-v3-turbo-q5_0.bin` (574 MB, SHA256 `394221709cd5ad1f40c46e6031ca61bce88931e6e088c188294c6d5a55ffa7e2`). Daha hafif: `ggml-small.bin` (488 MB, `1be3a9b2063867b937e64e2ec7483364a79917e157fa98c5d94b5c1fffea987b`). İkisi birlikte değiştirilir.
- **Ağ**: API'ye `http://whisper:8080/inference` (`Assistant__Speech__BaseUrl/Path` compose'da otomatik). Yerel geliştirme için yalnız `127.0.0.1:${WHISPER_PORT:-8178}` yayınlanır; üretimde bu port satırı kaldırılır.
- **Kaynak**: `WHISPER_MEMORY_LIMIT` (3g), `WHISPER_CPUS` (4). Healthcheck kök sayfa.
- **Gizlilik**: API sesi bellekte tamponlar (diske taşmaz), sunucuya bir kez gönderir; whisper.cpp `--convert` kullanılmadığından geçici dosya yazmaz. Tarayıcı kaydı cihazda 16 kHz mono WAV'a çevrilir.
- **Yedek**: `whisper-models` volume'u yedek kapsamında **gerekmez**; yeniden indirilebilir, kişisel veri içermez.

```bash
bash scripts/dev.sh speech        # imajı derler, modeli doğrular, servisi sağlıklı başlatır
bash scripts/dev.sh speech-stop   # durdurur (volume kalır)
docker volume rm buildingflow_whisper-models   # modeli tamamen silmek için (yeniden indirilir)
```

## Performans (07.11.2026 ölçümü)

macOS'taki Docker GPU'ya (Metal) erişemez; `large-v3-turbo-q5_0` 4 CPU ile istek başına ~35 sn sürdü. Aynı Mac'te Homebrew `whisper-server` (Metal) 0,24–1,2 sn. Öneri: Mac geliştirmede yerel `whisper-server`'ı kullanın (`ASSISTANT_SPEECH_BASE_URL=http://127.0.0.1:8178`); Docker profili Linux sunucularda daha fazla çekirdekle (`WHISPER_CPUS`) ya da daha hızlı `ggml-small.bin`/`ggml-base.bin` ile kullanılmalı. API zaman aşımı `Assistant:Speech:TimeoutSeconds` (varsayılan 60).
