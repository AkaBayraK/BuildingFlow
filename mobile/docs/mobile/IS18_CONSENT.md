# İŞ18 — Mobil rıza ekranları

16.10.2026, kullanıcı onayı: "diğer faza geçebilirsin". Ana plan ve task ayrıntısı: [PHASE_16_CONSENT_KVKK.md](../../../docs/analysis/PHASE_16_CONSENT_KVKK.md); sunucu sözleşmesi: [mobile-consents.md](../../../docs/api/mobile-consents.md). Bu iş ana yol haritasındadır (İŞ18), mobil faz sayacına eklenmez.

## Bağımlılık

Yeni paket yok; `package.json` ve `package-lock.json` değişmedi. Plandaki `react-native-webview` lisans kararı (16.10.2026) nedeniyle kullanılmadı. Metin, İŞ16'daki kısıtlı biçim ayrıştırıcısının mobil kopyasıyla React Native `Text` olarak çizilir. Karma için zaten kurulu `expo-crypto`, depolama için mevcut şifreli SQLite kullanılır.

## Dosyalar

| Dosya | Görev |
|---|---|
| `src/consents/markup.ts` | Ayrıştırıcı. Görünmez karakter denetimi Hermes'te `\p{}` gerektirmeyen açık Cc/Cf tablosuyla; `safeHref` yalnız `https:` ve `mailto:` kabul eder, `URL` kullanmaz. |
| `src/consents/contracts.ts` | Sunucu yanıtı ve komut doğrulayıcıları, durum etiketleri, `CONSENT_HOLD = 4031`. |
| `src/consents/store.ts` | `consent_cache`, `consent_jobs`, `consent_seen`; kuyruk, bekletilen işlerin serbest bırakılması, etkin durum hesabı. |
| `src/consents/engine.ts` | Liste yenileme (karma doğrulamalı) ve kuyruk gönderimi. |
| `src/consents/events.ts` | Değişiklik olayı ve SHA-256 yardımcı. |
| `src/consents/ConsentProvider.tsx` | Bağlam, açılış kapısı, `useConsentPrompt().ask(feature)`. |
| `src/consents/MarkupView.tsx`, `ConsentSheet.tsx` | Metin çizici ve tam ekran rıza sayfası. |
| `src/components/ConsentCenter.tsx` | Profil → Rızalarım. |
| Değişen | `offline/database.ts` (şema 8), `sync/engine.ts`, `sync/outbox.ts`, `profile/engine.ts`, `profile/outbox.ts`, `profile/useProfile.ts`, `auth/AuthProvider.tsx`, `app/_layout.tsx`, `app/(tabs)/profile.tsx`, `components/ProfileEditor.tsx`, `RequestForm.tsx`, `SyncPanel.tsx`. |

## Davranış

- Fotoğraf eklemek için (profil ve talep) önce ilgili açık rıza istenir. Kişi vazgeçerse fotoğrafsız devam eder.
- Onay kutusu işaretlenmeden onay düğmesi pasiftir. Aydınlatma metni "okundu" olarak kaydedilir, geri çekilemez.
- Çevrimdışı karar şifreli kuyruğa yazılır, bağlantı gelince talep ve profil eşitlemesinden önce gönderilir. Metin değişmişse (409) kişiye yeni sürüm gösterilir.
- Sunucu fotoğraflı işi rızasız diye reddederse iş silinmez, "rıza bekliyor" olarak durur; rıza verilince otomatik yeniden gönderilir veya kişi iptal eder.
- Açılış kapısı okunmamış aydınlatma metnini ve yeni sürümü yayınlanmış açık rızayı her sürüm için bir kez gösterir.
- Rızalarım güncel durumu gösterir; geçmiş yalnız merkezdedir. Geri çekmede bu cihazdaki ilgili fotoğraflar silinir.

## Kanıt

- `tests/consents.test.mjs` 13/13; `npm run check` 105/105; `npm run build` çıkış 0; lisans kontrolü 329 PASS.
- Gerçek İŞ17 API yanıtları mobil motordan geçirildi.
- Expo web önizleme ekranları: `/mnt/project-files/analysis/is18-screens/`.

## Açık (T18.6)

Gerçek iPhone/Android cihazda: çevrimiçi onay, uçak modunda onay + fotoğraf ve bağlantı gelince sıralı gönderim, merkezde yeni sürüm yayınlayıp kapının açılması, geri çekme sonrası fotoğrafın kalkması.
