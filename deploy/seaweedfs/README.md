# BuildingFlow SeaweedFS4.48

Apache-2.0 lisanslı upstream imaj sürümü ve digest Compose içinde sabittir. İç veri servisi seaweedfs, yalnız internal storage-data ağındadır; master/filer/volume portları yayımlanmaz. seaweedfs-s3 özel kimlik dosyasıyla API/monitoring storage ağına ve Mac127.0.0.1:9040 portuna S3 sunar. Mobilde S3 anahtarı veya doğrudan nesne URL'si bulunmaz; mobil merkez API'sini kullanır.

scripts/configure-object-storage.py .env'deki OBJECT_STORAGE_ACCESS_KEY/SECRET_KEY ile0600 private-s3.json üretir. Bu dosya Git'e girmez ve S3 gateway'e salt okunur bağlanır. Admin/WebDAV arayüzü kapalı, IAM yazımı kapalı, dolu bucket silme kapalıdır. Mini modu yerel geliştirme topolojisidir; çok düğümlü üretim HA kurulumu bu bakımın kapsamı değildir.

Yeni kurulum: bash scripts/init-local.sh; docker compose --env-file .env -f deploy/docker-compose.yml up -d seaweedfs-s3.
S3 bucket policy beklenmedikse API503 ile kapanır; anonim S3403 olur. Dosya veri/rol/yetki ve SHA256 kontrolleri mevcut API'de korunur.
Yedek: docs/BACKUP_RESTORE.md format2. MinIO→SeaweedFS taşıma: scripts/migrate-object-storage.py kaynak dosyaları değiştirmez; hedef çakışmasını reddeder ve SHA256 doğrulaması olmadan başarı raporu oluşturmaz.
