# Yedekleme ve geri dönüş

İŞ11 adım03. Araç: [scripts/backup_restore.py](../scripts/backup_restore.py); prova: [scripts/test-backup-restore.py](../scripts/test-backup-restore.py). Kanıt: [TESTING.md](TESTING.md).

## Neyin yedeği alınır

| Parça | Yöntem | Not |
| --- | --- | --- |
| Uygulama PostgreSQL (`postgres`) | `pg_dump --format=custom --no-owner --no-acl` | Dump öncesi/sonrası tablo satır parmak izi aynı olmalı |
| Keycloak PostgreSQL (`keycloak-db`) | aynı | Kullanıcı, realm, istemci ve kimlik bilgisi hash'leri burada |
| MinIO verisi (`/data` named volume) | Durdurulmuş volume'un salt-okunur `tar.gz` cold snapshot'ı | Nesne bazında SHA256 listesi manifest'e yazılır |

SQL dump tek başına yeterli değildir: belge metadata'sı uygulama DB'sinde, dosyanın kendisi MinIO'da, kullanıcı kimliği Keycloak DB'sindedir. Üçü aynı bakım penceresinde alınır.

## Yedek alma (bakım penceresi)

1. API, worker, web, Keycloak ve MinIO'yu durdurun; yerel `dotnet run` süreçleri dahil başka yazıcı kalmamalı. PostgreSQL konteynerleri çalışır kalır.
2. Yedek klasörü yeni olmalı; araç var olan klasörün üzerine yazmaz.

```bash
python3 scripts/backup_restore.py backup --confirm-maintenance --output /güvenli/konum/buildingflow-2026-10-11
```

3. Çıktı: `application.dump`, `keycloak.dump`, `minio.tar.gz`, `manifest.json` (SHA256 + DB parmak izleri + nesne listesi). Klasör `0700`, dosyalar `0600`. Hata olursa `INCOMPLETE` işareti bırakılır; bu klasör geri yüklenemez.
4. Servisleri yeniden başlatın. Yedekler kişisel veri ve parola hash'leri içerir: şifreli ortamda saklayın, Git'e, CI artifact'ine veya paylaşımlı klasöre koymayın.

Araç çalışan yazıcı konteyner görürse veya dump sırasında DB parmak izi değişirse durur.

## Yedeği doğrulama (geri dönüş provası)

```bash
python3 scripts/backup_restore.py restore-check /güvenli/konum/buildingflow-2026-10-11
```

Manifest, sürümler ve tüm SHA256'lar kontrol edilir; ardından **yalnız yeni oluşturulan** `buildingflow-test-restore-*` PostgreSQL konteyneri, internal ağ ve volume'a geri yüklenir, DB parmak izleri ve nesne hash'leri karşılaştırılır, kaynaklar silinir. Mevcut veritabanı veya volume hedef olarak kabul edilmez.

## Gerçek geri dönüş

Restore her zaman **yeni ve boş** bir hedefe yapılır; canlı volume'un üzerine yazılmaz. Önce `restore-check` geçmelidir. Ardından yeni PostgreSQL örneğinde iki boş DB'ye `pg_restore --no-owner --no-acl --exit-on-error`, yeni bir MinIO volume'una `tar -xzf`; uygulama bağlantıları yeni hedefe çevrilir ve doğrulandıktan sonra eski ortam emekliye ayrılır. Migration'lar yedekteki sürümden ileri gitmez; uygulama sürümü yedeğin alındığı sürümle eşleşmelidir.

## Sınırlar

- Bu turda canlı/yerel kullanıcı verisinin yedeği alınmadı ve hiçbir canlı hedefe restore yapılmadı; doğrulama kendi fixture'ıyla yapıldı.
- Yedek tutarlılığı bakım penceresine dayanır (online/PITR yok). Saklama süresi, şifreleme anahtarı yönetimi ve uzak kopya hedef ortamda belirlenir.
- Redis ve RabbitMQ yedeklenmez: önbellek ve geçici kuyruklardır; kalıcı mesaj durumu PostgreSQL outbox/inbox tablolarındadır.
