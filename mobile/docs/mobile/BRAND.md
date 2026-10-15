# BuildingFlow marka bakım teslimi — 14.10.2026

Kullanıcının paylaştığı üç bina simgesi kodla yeniden çizildi. Referansın lacivert, mavi ve turkuaz çizgileri korunarak yuvarlak birleşimler ve küçük boyutta okunabilir geometri kullanıldı. Raster ekran görüntüsü büyütülmedi; ortak kaynak mobile/assets/brand/mark.json'dur. Bu ayrı marka bakım talebidir; Faz07 başlatılmadı.

## Kullanım

- primary: açık zeminlerde renkli ana marka; mobil ve merkez için önerilen sürüm.
- light: koyu mobil başlıklarda açık renkli aynı simge.
- mono: tek renk kullanım.
- iOS ikon: 1024×1024, opak açık zemin; Android adaptif ön plan: 1024×1024 transparan, maske güvenli alanında.
- React Native BuildingFlowMark, giriş ekranı ve Screen başlığında kullanılır. app.json ikon/favikon yollarını yeni varlıklara bağlar.
- Merkez WorkspaceShell, yeni React SVG bileşenini kullanır. Çıkış sayfası ve Keycloak teması buildingflow-logo.svg kullanır. Genel bina/daire işlev ikonları korunur.

SVG üretimi: mobile klasöründe npm run brand:generate. PNG'ler bu SVG'lerin Sharp ile rasterleştirilmiş çıktılarıdır; geometri değişirse PNG'ler de yeniden üretilmelidir. Ana geometri merkezde src/brand/buildingflow-mark.json olarak aynalanmıştır; sonraki değişikliklerde bu dosya ve public/Keycloak SVG çıktıları aynı üretimden güncellenmelidir. Mevcut eski PNG'ler silinmedi.

## Doğrulama

- Mobil TypeScript/lint ve26/26 mevcut test geçti; SVG web accessible=false uyarısı görsel kontrolde bulunup kaldırıldı.
- iPhone development build:0hata,1 Xcode script dependency-analysis uyarısı. Fiziksel iPhone'a yüklendi; güncel Metro URL'siyle devicectl launch başarılı. Telefonda ekranı uzaktan görüntüleme olanağı bulunmadığından fiziksel görsel kabul iddiası yoktur.
- Chrome390×844 mobil giriş: logo görünür, yatay taşma yok. Kanıt brand-mobile-login.png.
- Merkez TypeScript/Next production build geçti. Ayrı yerel3004 önizlemede SVG tam yüklendi; kanıt brand-central-login.png. Ana3003 Docker web servisi yeniden dağıtılmadı.
- react-native-svg15.15.4: MIT; Expo57 uyumlu sürüm expo install ile kuruldu. Üst seviye paket lisans metadata envanteri706, belirsiz0. Önceki28 npm audit bulgusu giderilmiş sayılmaz.
- Faz06 backend52/52 seçili integration ve fiziksel SQLCipher fixture kanıtları PHASE06.md'de; marka değişikliği nedeniyle backend iş testleri yeniden çalıştırılmadı.
- Expo Doctor21/21 kontrol geçti; son düzeltmeden sonra TypeScript/lint ve üç platform export yeniden başarılı. API5006 ve Metro8081 health200 doğrulandı.
- Android/iOS mağaza yayını, commit/push yok. Android native çalışma testi yapılmadı.

İlerleme: marka bakım3/3 adım (%100): vektör/varlıklar, mobil-merkez kod bağlantıları, derleme/görsel kontrol ve belge. Mobil ürün yol haritası15fazdır; yeni faz sayacı/iş roadmap kaydı oluşturulmadı. Token/context ölçümleri: BİLGİ MEVCUT DEĞİL.

## Son kullanıcı düzeltmesi
Merkez sol üst simge mobil light paletiyle (#F5FAFF/#69BFE4/#8BD0D6) eşitlendi; mobil giriş ve Screen başlığındaki Building beyaz, Flow merkezdeki #00cafa olarak güncellendi. Merkez web Docker derlemesi/typecheck geçti ve yalnız web3003 yeniden oluşturuldu; gerçek girişli ekranda5 path rengi doğrulandı. Mobil TypeScript/lint geçti. Kanıt brand-central-matched.png / brand-mobile-matched.png. Kullanıcı görünümü “oldu ok” ile onayladı.
