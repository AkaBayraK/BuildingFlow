# Mağaza bilgisi, gizlilik ve ekran görüntüsü — açık kabul listesi

Bu belge bir gizlilik politikası, hukuki açık rıza metni veya gönderilmiş mağaza formu değildir. Ürün sahibi üretim/mağaza bilgilerini henüz vermedi. Aşağıdaki alanlar onaylanmadan readiness passed yapılmaz.

## Hesap ve metadata

- [ ] AppleDeveloper/GooglePlay/Expo hesap sahipliği,uygun rol ve ücret/hesap durumu.
- [ ] Kalıcı iOS/Android kimliği,gerçek ExpoProjectUUID,publicclient ve HTTPS domain/TLS.
- [ ] Onaylı görünen sürüm,ilk veya mevcut mağazaya göre artan buildnumaraları.
- [ ] Uygulama adıBuildingFlow,kısa/uzun açıklama,kategori,yaş/içerik derecesi,destek ve gizlilik URL’si,irtibat ve dağıtım bölgeleri.
- [ ] İnceleme için kişisel veri içermeyen minimum yetkili fixture hesap ve erişilebilir test ortamı. Parolalar Git/docs içine yazılmaz; mağazanın güvenli inceleme alanı kullanılır.
- [ ] Hesap/veri silme talebi,merkezden fotoğraf kaldırma,saklama/yedek süreleri ve hizmet sağlayıcıları ürün/hukuk tarafından açıklanır. Uygulamada olmayan hesap silme hizmeti varmış gibi beyan edilmez.

## Teknik veri akışını beyana dönüştürmeden önce inceleyin

| Veri | Kodun mevcut davranışı | Tamamlanacak karar |
| --- | --- | --- |
| Hesap kimliği/ad/iletişim | OIDC kullanıcı kimliği,profil adı/soyadı ve isteğe bağlı telefon merkezle eşitlenir | Bağlılık,amaç,saklama ve silme prosedürü |
| Fotoğraflar | İsteğe bağlı profil/talep fotoğrafı özel merkez depolamasına gönderilir; cihazda şifreli kopya | Fotoğraf/medya beyanı,sağlayıcı,yedekler ve kaldırma süresi |
| Mülk/daire/rol | Yetkili API kapsamı ve yerel cache | Adres/kimlik ilişkisi ve erişim amaçları |
| Aidat/borç/mahsup | Yetkili finans bilgisi gösterilir; kart/banka hesabı girilmez | Finansal veri sınıfları ve sunucudaki gerçek veri akışı; “ödeme yok” diye tüm finans verisini yok saymayın |
| Talep/duyuru/kişisel mesaj/okundu | Açıklama,kategori,öncelik,mesaj ve okundu merkezle eşitlenir | Kullanıcı içeriği/iletişim/etkileşim beyanları |
| Token/cache | Access bellekte,refreshSecureStore;SQLCipher/AES kopya | Güvenlik amacı ve kalıcılık; “yalnız yerel” tüm profil akışına uygulanmaz |
| SDK/telemetri | Ayrı runtimeSDK ve sunucu/hosting logları vardır | Son imzalı pakette SDKprivacy manifestleri,üçüncü taraf aktarım ve gerçek log/izleme davranışı |

Kamera/galeri açıklamaları profil ve talep fotoğrafını kapsar. Runtime’da mikrofon/konum/push izni eklenmedi. Android CNG çıktısında RECORD_AUDIO ve üretimoverlay remove kuralları bulunur; kütüphane manifestlerinin birleştiği son AAB ayrıca taranmalıdır. Nativepicker’ın eskiAndroid depolama izinleri varsa gerçek SDK/OS davranışı beyanla karşılaştırılır. İzin listesi veri toplama beyanının yerine geçmez.

- [ ] AppStore AppPrivacy ve GooglePlay DataSafety,uygulama/SDK/sunucu gerçek davranışıyla doldurulur; “hiç veri toplanmıyor” otomatik işaretlenmez.
- [ ] PrivacyPolicy URL’si,hukuki metin,saklama/silme/üçüncü taraf ve bölgeler onaylanır; uygulamadaki mevcut açıklama nihai metin yerine sunulmaz.
- [ ] iOSPrivacyInfo.xcprivacy/required-reasonAPI/SDK imzaları final IPA’da kontrol edilir; bu tur hayali reason kodu eklenmedi.
- [ ] SQLCipher/AES/TLS için Appleexportcompliance soruları yetkili kişi tarafından cevaplanır. usesNonExemptEncryption otomatik false yapılmadı; muafiyet varsayılmadı.
- [ ] Tracking/reklam davranışı son SDK’larla kontrol edilir; ATT veya “trackingyok” beyanı incelemesiz eklenmez.

Kaynaklar: [Appleprivacy](https://developer.apple.com/app-store/app-privacy-details/), [GoogleDataSafety](https://support.google.com/googleplay/android-developer/answer/10787469), [Appleexport](https://developer.apple.com/help/app-store-connect/manage-app-information/overview-of-export-compliance/). Bunlar güncel formun yerine doldurulmuş cevap değildir.

## Ekran görüntüleri ve cihazlar

- [ ] Güncel mağaza panelinin istediği cihaz/çözünürlük/adet değerlerini alın: [Apple screenshot specification](https://developer.apple.com/help/app-store-connect/reference/screenshot-specifications/). Sabit eski ölçüler bu belgeye kopyalanmadı.
- [ ] iOSsupportsTablettrue nedeniyle iPad kabulünü ve panelin iPadgörsel ihtiyacını değerlendirin; sadeceiPhone testinden iPad sonucu çıkarılmaz.
- [ ] Gerçek release binary’den fixture verilerle giriş/ana sayfa,finans,talep,bildirim,profil akışlarını çekin. Prototip veya başka ürüne ait ekran yüklenmez.
- [ ] Onaylı aynıBuildingFlowlogo/isim,splash veadaptiveicon; gerçekcoldstart ve sistem maskeleri.
- [ ] Küçük/büyükekran,%200metin,VoiceOver/TalkBack,klavye,izin kabul-ret,uçakmodu ve hesapçıkışı matrisi.
- [ ] Push,onlineödeme veya sözleşme gibi henüz bağlı olmayan özellikleri mağaza metninde çalışıyor diye tanıtmayın.

## Yayın engelleri

Gerçek üretim kimliği/sürümü/HTTPS/proje ve hesaplar eksik. Readiness6alanpending. Güncel audit29(18high/11moderate),4kökadvisory açık; öncekiFaz14kısmi nativeURLguard tamdecoder/web çözümü değildir. Lisans ifadeleri tarandı ancak dağıtım yükümlülüğü ve gerçeknativeartefakt kabulü açık. Bu listeden hiçbir madde bu tur otomatik ürün/hukuk/mağaza onayı sayılmadı.
