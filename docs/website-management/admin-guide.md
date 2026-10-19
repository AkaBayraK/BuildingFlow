# Web Sitesi Yönetimi — Yönetici kullanım rehberi

19.10.2026. Yönetim panelinde **Web Sitesi Yönetimi** menüsünü açın ve işlem yapacağınız yönetim alanını seçin. Giriş hesabı ile kişi/malik/kiracı kaydı aynı şey değildir; yetki backend güncel görev/üyelik/ilişki kapsamından gelir.

## Siteyi hazırlama

1. Henüz site yoksa site anahtarı ve görünen adıyla oluşturun. Site anahtarı public URL parçasıdır; başka alanın anahtarını kullanamazsınız.
2. Ana sayfa ve zorunlu KVKK/çerez sayfalarını doldurun. Yer tutucu metinlerle yayın yapılamaz. Demo içerik gerçek site verisi değildir.
3. Sayfalarda başlık, menü görünürlüğü, sıra ve SEO alanlarını düzenleyin; blok ekleme/düzenleme/sıralama ortak panellerdedir. HTML çalıştırılmaz. Mükerrer LOGIN_CARD geçmiş kayıtları silinmez; premium şablonda giriş üstte tek **Hesabıma Giriş** bağlantısıdır.
4. Tema renklerini ve hakları doğrulanmış logo/hero/galeri görsellerini seçin. Renk kontrastı backend'de kontrol edilir.
5. Medya yüklerken kullanım hakkı ve gerekiyorsa kişi iznini doğrulayın. JPEG/PNG imza, boyut ve piksel sınırları uygulanır; metadata temizlenir. Kimlik veya özel malik belgesini public galeriye eklemeyin.

## Önizleme ve yayın

Taslak önizlemesi yetkilidir; tam önizleme kısa süreli HMAC ile ayrı sayfada açılır, noindex/no-store'dur. Bağlantı paylaşılabilir kalıcı public yayın değildir. Yeni düzenleme eski önizlemeyi geçersiz kılar; yeniden açın. Sunucu preview anahtarı yoksa kapalı davranır; operatörünüzle iletişime geçin.

İçerik düzenleme SUPER_ADMIN/SITE_ADMIN/MANAGER görev kapsamındadır. Yayın ve geri yükleme SUPER_ADMIN/SITE_ADMIN kapsamındadır; siteyi askıya alma SUPER_ADMIN içindir. Yayın geçmişinde önceki sürümü geri yüklemek yeni yayın üretir; audit/geçmiş kaybolmaz. Aynı kaydı başka biri değiştirdiyse409 durumunda güncel kaydı yükleyin, alanları kontrol edip yeniden kaydedin; kör tekrar yapmayın.

Public yayını gerçek `/{siteKey}` yolunda kontrol edin. Bilinmeyen/taslak/askıdaki site404 döner. API hatasıyla içerik yokluğunu aynı kabul etmeyin. Public sitede iç borç/kişi/duyuru/belge verisi yer almaz.

## Alan adı

Alan Adları panelinde custom veya platform alt alan adını ekleyin. Bir kez gösterilen TXT kaydını güvenli şekilde DNS operatörüne iletin; tokenı loga/Git'e koymayın. DNS ve TLS kontrolünü çalıştırın. Custom domain ayrıca SUPER_ADMIN onayı ister. Bütün kapılar doğrulanmadan primary açılamaz; bir site tek primary kullanır. Ayrıntı ve24saat kontrolü [domain rehberi](../api/website-domains.md).

## İlanlar ve başvurular

İlanı isteğe bağlı aynı alandaki bağımsız bölüme bağlayın; bu malik/kiracı ilişkisini değiştirmez. Özel yetki belgesi ve fotoğrafları doğru alanlarda kullanın. Taslak → gönder → inceleme/onay → yayın sırasını takip edin; değişiklik yeniden inceleme gerektirebilir. Gerçek EİDS doğrulaması ve hukuki kabul olmadan yayın kapalıdır; onay kutusu veya yönetici beyanı bu kapıyı açmaz.

Başvurular kişisel veri içerir; yetkisiz paylaşmayın. Gerçek aydınlatma metni ve operatörün HMAC/proxy yapılandırması hazır değilse form kapalı olabilir. Saklama/anonimleştirme prosedürü [ilan sözleşmesinde](../api/listings.md), açık kabul [#30](https://github.com/AkaBayraK/BuildingFlow/issues/30) içindedir.

## Kullanıcı girişi

Herkes public sitedeki tek **Hesabıma Giriş** bağlantısını kullanır; giriş yeni sekmede açılır, website sekmesi korunur. Yönetici/malik/kiracı/sakin seçimi giriş ekranında yapılmaz; hesap girişinden sonra yalnız o site için güncel yetkili çalışma alanları gösterilir. Kök demo genel yönetim girişine gider. Özellik kapalı veya eşleşen ilişki yoksa erişim verilmez; yeni hesap/rol kendiliğinden oluşturulmaz. [Giriş sözleşmesi](../api/website-login.md).
