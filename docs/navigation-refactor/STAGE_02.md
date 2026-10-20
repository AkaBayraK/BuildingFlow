# SIDEBAR FAZ07.1 — Aşama02 teslimi

19.10.2026. Kaynak: buildingflow-sidebar-phase/PHASE_07_1.md, Aşama02/08. Yalnız bu aşama onaylandı; Aşama03 başlamadı.

**Tipli NavigationItem/NavigationGroup/NavigationContext ve NavigationModel, merkezi gerçek rol/bayrak görünürlüğü, property kapsamlı hedefler, nested aktiflik ve duplicate denetimi uygulandı.** Sözleşme WorkspaceShell sidebar, arama kaynakları ve alt kısayollar tarafından tüketilir. İkonlar mevcut Lucide kayıtlarıyla eşlenir; etiketler mevcut TR/EN namespace anahtarlarıdır. Disabled öğelerin href taşıması tip düzeyinde yasaktır. Mevcut görsel gruplar, stiller, URL ve auth akışı korunur; sonraki aşamanın yeni gruplaması/arama algoritması/profil/drawer davranışı uygulanmadı.

`navigationPermissions` yalnız mevcut shell görünürlüğüdür. Realm rolü aktif üyelik/görev izni anlamına gelmez; backend CurrentUserService/WorkspacePropertyPermissions ve API yetkisi son otoritedir. sessionContexts açıkken Kullanıcılar/Açık Rıza platform koşulları korunur; centralPeople seçimi /people hedefini korur. Bilinmeyen roller kişisel/finans menüsü açmaz. Property tipi yalnız aynı ID ile yüklenmiş kayıttan alınır; apartmana bina eklenmez. Planlı Kullanıcılar ayrı ID ile disabled kalır; yeni hedef veya yetki uydurulmaz.

Değişen uygulama/test dosyaları:
- frontend/buildingflow-web/src/features/navigation/navigation.ts (yeni)
- frontend/buildingflow-web/src/components/WorkspaceShell.tsx
- frontend/buildingflow-web/tests/navigation.test.mjs (yeni)
- frontend/buildingflow-web/tests/dashboard.test.mjs (kaynak metni yerine gerçek modelin menü sırası/kısayolu da denetlenir)

**167/167 frontend testi, 0 başarısız/atlanmış; npm run typecheck ve npm run build GEÇTİ.** Altı yeni test rol matrisi, çoklu rol/flag, scope/tip, nested aktif/parent, disabled/route ve duplicate davranışlarını doğrular; duplicate matrisi tüm16rol kombinasyonu×4flag kombinasyonunu kapsar. İlk typecheck union/link ayrımı hatası düzeltilip geçti. İlk npm test yanlış kökte package.json bulamadı; frontend klasöründe gerçek suite çalıştırıldı. İlk suite166/167: her rotanın OIDC allowlistinde olduğu varsayımı mevcut scoped SMS istisnasında yanlış çıktı; gerçek kısıt açık assertion ile kaydedildi. Son suite167/167. İlk build sandbox SWC cache hatası; aynı komut izinli tekrar ile başarılı. Build'in next-env üretimi başlangıç hashine birebir döndürüldü; iki önceden mevcut next-env farkı korundu.

Açık sınırlamalar: mevcut `/properties/{id}/sms` güvenli OIDC dönüş listesinde yok; `/` mevcut validator tarafından kabul edilmiyor. Bu aşamada auth allowlist genişletilmedi. Türkçe arama, mobil aria/drawer, yeni gruplama, tooltip ve footer sonraki aşamalardır. Canlı oturum, gerçek rollerle tarayıcı, offline/cihaz ve backend entegrasyon testleri bu tur ÇALIŞTIRILMADI. Build ve saf model testleri bunların kabulü değildir. Web manifestinde lint scripti yok.

Yeni paket/font/ikon/görsel, API/DB/migration veya mobil değişikliği yok. Manifest/lock başlangıç SHA256 değerleriyle aynı; LICENSE/NOTICE korunur. Restore/npmci olmadığı için lisans envanteri yeniden üretilmedi; önceki dağıtım lisans sınırlamaları açık kalır. Tek agent; phase/contract/verify/docs/license kullanıldı, yeni skill/MCP kurulmadı.

**Aşama02 tamamlandı: bu tur1/1=%100,00; sidebar2/8=%25,00.** Genel İŞ/UI sayaçları değiştirilmedi: milestones.ts içindeki İŞ07.1 hata mesajları farklı iştir, sidebar görevi olarak işaretlenmedi. Kanıt docs/TESTING.md ve bu teslim raporunda aynı SIDEBAR FAZ07.1 kimliğindedir. Genel iş yol haritasının güncel toplamı BİLGİ MEVCUT DEĞİL.

Token giriş: BİLGİ MEVCUT DEĞİL; çıkış: BİLGİ MEVCUT DEĞİL; toplam: BİLGİ MEVCUT DEĞİL; kalan context: BİLGİ MEVCUT DEĞİL. Codex usage19.10.2026:300dk kullanılan%17/kalan%83 reset20.10.2026 00:50:09;10080dk kullanılan%97/kalan%3 reset25.10.2026 00:15:34 Europe/Istanbul. Hesap kotası sohbet tüketimi değildir.

Sonraki adım: Aşama03 için kullanıcı onayı; teknik engel yok.
