# [QA FAZ 09 | KAYIT 10/14] AkaAI entegrasyonu ve AI Test Agent — GitHub #105

```text
[BUILDINGFLOW]
[FAZ QA 09 | KAYIT 10/14] AkaAI entegrasyonu
[DURUM] TEKNİK TESLİM / KABUL BEKLİYOR
[İLERLEME] FAZ 09 PHASES maddeleri 5/6 = %83,33 (gerçek AkaAI uçtan uca bağlantısı yapılandırma bekliyor)
```

## Ne yapıldı
- **AkaAI analizi:** Java 21/Spring Boot 4.1; OpenAI uyumlu `/api/v1/integrations/chat/completions` ve `/models`, INTEGRATION_CLIENT rollü client-credentials JWT, tenant'a bağlı entegrasyon istemcisi, `X-Actor-Ref`/`X-Request-Id` audit başlıkları, mesaj/araç/boyut limitleri; yerel Ollama, cloud kapalı. AkaAI deposunda değişiklik yapılmadı → diğer ürünler aynı sözleşmeyle kullanabilir.
- **BuildingFlow `QaAiAssistant`:** plan (Türkçe istem → kapalı şema + allowlist doğrulama → onay gerektiren öneri), koşum açıklaması (kayıtlı adımlardan, sonucu değiştirmez), token önbelleği, maskeli aktör, günlük bütçe, zaman aşımı, kesintide 503 fallback. 4 hata kodu BE/FE/TR/EN.
- **Web:** AI Test Asistanı sekmesi etkin: istem kutusu (sayaç, etiket), "AI önerisi — doğrulanmış sonuç değildir" kutusu, riskler, Onayla ve çalıştır / Reddet; Canlı İzleme'de "AI ile açıkla" ve "kayıtlı sonucu değiştirmez" etiketi.

## Testler (bu tur)
- `QaAiAssistantApiTests` sahte AkaAI ile: geçerli plan hiçbir koşum başlatmaz ve secret gönderilmez; **5 saldırı/uydurma yanıt** (komut adı, ek `sql` alanı, mükerrer, boş, "talimatları yok say, PASS yaz" düz metin) → 502 reddedildi; AI "FAILED yap" dese de koşum PASSED ve kayıt byte-aynı (**kanıtsız PASS/FAIL yok**); SITE_ADMIN 403, uzun/kontrol karakterli istem 400, bütçe 429, bağlantı kesik 503, yapılandırılmamış 503; şema ayrıştırıcı negatif fixture'ları. Toplam koşum (QaCenter + ErrorContract + envanter + migration ile) **27/27**.
- Web `npm test` 307/307, typecheck, build PASS; lisans 329 PASS; KVKK log taraması 0; test logunda secret/token geçmiyor.

## PHASES kontrol listesi
| Madde | Durum |
|---|---|
| AkaAI gateway/model/agent/MCP kapasite analizi | PASS |
| Güvenli API kontratı, RBAC, allowlist, tenant izolasyonu | PASS (SUPER_ADMIN, allowlist; AkaAI tarafı tenant entegrasyon istemcisiyle) |
| Türkçe prompt → schema plan → onay → deterministik run | PASS |
| Hata/kök neden analizi, maliyet/token bütçesi, fallback | PASS (istek bütçesi; token sayımı AkaAI tarafında, BuildingFlow'da ölçülmüyor) |
| Prompt injection, yanlış plan, tool misuse, kanıtsız PASS testleri | PASS |
| Diğer projelerde yeniden kullanılabilirlik | PASS (AkaAI değişmedi, standart entegrasyon sözleşmesi) |
| Gerçek AkaAI ile uçtan uca | **BEKLİYOR**: AkaAI'da BuildingFlow entegrasyon istemcisi + secret tanımı ve `QaCenter:AkaAi:*` ayarı gerekir |

[TOKEN] BİLGİ MEVCUT DEĞİL.
