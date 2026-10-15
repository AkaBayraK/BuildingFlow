# SeaweedFS4.48 depolama bakımı — 14.10.2026

Kullanıcı Faz07 öncesinde MinIO yerine SeaweedFS4.48 istedi. Ayrı BuildingFlow veri/gateway servisleri kuruldu; AkaAI'nin SeaweedFS4.30 ortamı değiştirilmedi.

## Uygulanan değişiklik
- Minio7.0.0 paketi ve MinioFileStorage kaldırıldı. IFileStorage sözleşmesi korunarak AWSSDK.S34.0.104.3 ile S3FileStorage eklendi.
- Bucket gizlilik kontrolü, JPEG/PNG/PDF tür/boyut, API rol/üyelik ve SHA256/boyut doğrulaması; UUID/fingerprint replay ve soft-delete davranışı korunur. SeaweedFS'in NoSuchBucketPolicy404 XML yanıtının SDK tarafından Policy olarak döndürüldüğü durum yalnız doğrulanmış hata kodu/durum için ele alınır. DTD/dış kaynak ve beklenmeyen politika reddedilir.
- SeaweedFS4.48 resmi imaj digest'i sabit. İç veri servisi yalnız internal storage-data ağında; sadece authenticated seaweedfs-s3 API/monitoring ağına ve127.0.0.1:9040 portuna açılır. Admin/WebDAV UI, IAM yazımı ve dolu bucket silme kapalı. SDK yalnız configured private endpoint'e bağlanır; AWS bulutu kullanılmaz.
- ObjectStorage ayarları server.env'de;0600 private-s3.json salt okunur bağlanır ve Git'e girmez. Mobil başlatıcı merkezi sırları yalnız API sürecine yükler; Expo public ayarlarına sır konmaz.
- API,worker,web imajları derlendi ve yerel servisler yenilendi; MinIO konteyneri kaldırıldı. Eski buildingflow_minio-data volume veri kaybını önlemek amacıyla saklanır. Eski bağlantı yalnız artifacts/storage/legacy-source.env0600 geri dönüş kaydında; aktif.env'den çıkarıldı.
- Geçiş, API/native yazıcıları durdurularak source-preserving S3 kopya aracıyla doğrulandı. Eski depoda0bucket/0nesne bulundu; taşınmış gerçek kullanıcı dosyası varmış gibi sunulmaz. artifacts/storage/migration-report.json doğrulanmış rapordur. Araç hedef içerik çakışmasında üzerine yazmaz; liste ve SHA256 tekrar kontrolü olmadan başarı raporu üretmez.
- Yedek format2: seaweed.tar.gz + özel storage-auth.json + PostgreSQL/Keycloak dump/parmak izi. IAM anahtarları, yeniden başlatmada anonim S3 oluşmaması için özel auth snapshot'ı ile korunur. Eski MinIO format1 arşivi SeaweedFS volume'una yüklenmez; kaynakta izole S3 taşıma gerekir.

## Gerçek doğrulama
96/96 seçili API/integration, atlama0: dosya round-trip/private bucket, negatif yetki, UUID replay/çift gönderim, bozuk içerik, beklenmeyen politika; mobile/session/sync, model/audit ve release güvenliği. Tüm backend regresyonu bu bakımda çalıştırılmadı.
104/104 frontend; TypeScript ve Docker Next build geçti. Testte önceki mobil sync hata kodunun eksik TR/EN karşılığı bulundu ve ortak FE katalog/sözlüklerine eklendi.
8/8 izole yedek/restore:3objeninSHA256'sı,2DBparmak izi, Keycloak realm/user, anonim403, bozuk/eksik/path traversal arşiv reddi, sahip olunan test kaynaklarının temizliği ve canlı servislerin değişmezliği.
Release preflight24negatif senaryo;329npm/NuGet lisans metadata, belirsiz0. SeaweedFS Apache-2.0; AWS SDK LICENSE/NOTICE API/worker'a dahil. Container OS paketlerinin tamamının aynı lisans olduğu veya hukuki uyumluluk garantisi iddia edilmez.
Yerel smoke başarılı: web/API/DB, korumalı/bozuk JWT401, S3403, Keycloak, broker,Redis,Grafana,worker. Mobil API5006/Metro8081health200. MainAPI localhost5005health200;127.0.0.1Host doğrulamasında400 ayrı beklenen kısıttır.

DB entity/migration veya kullanıcı parola değişimi yok. Gerçek kişisel fotoğraf, belge veya mesaj yazılmadı; commit/push/mağaza yayını yok. Yerel mini+gateway topolojisi üretim HA kurulumu değildir.
Faz07 fotoğraf ürün kodu bu tur tamamlanmadı; onaylı akış kardeş mobil repo docs/mobile/PHASE07.md'de. İş/UI roadmap sayacı değişmedi; bu ayrı depolama bakım talebidir.

Son kabul: mobil TypeScript/lint26/26 geçti; merkezde gerçek PKCE hesabıyla giriş sonrası API erişilebilir durumu görüldü. Ekran kanıtı kardeş mobil repo docs/mobile/storage-refactor-center.png. Prometheus object-storage-auth probe_success=1; veri servisi yalnız internal storage-data ağında ve yayımlanan port yok. API/worker imajlarında Minio.dll bulunmadı; AWSSDK.S3.dll bulundu.
