# İŞ 07 — Gider ve bütçe takibi

Onay: 10.10.2026 kullanıcı talebi. İş planında kayıt 09/14; dört resmi görev korunur. UI 07 ayrı geniş tasarım aşamasıdır; bu tur iş 07 için gereken gerçek ekranları kapsar.

## Kapsam ve kararlar

- Sekiz sabit gider kategorisi: bakım, temizlik, personel, enerji, güvenlik, sigorta, yönetim, diğer. Ayrı kategori CRUD bu fazda yok.
- Expense yönetim alanına ait; TRY pozitif iki ondalık, gerçekleşme tarihi (Europe/Istanbul, geleceğe kayıt yok), açıklama ve opsiyonel belge numarası/referansı. Dosya yükleme Faz 08.
- Gider tutarı sonradan üzerine yazılmaz: version/gerekçeyle tek iptal; düzeltme için yeni kayıt. İptal tutarı ve geçmişi saklanır. POST UUID + normalize fingerprint ile tekrar güvenli.
- PropertyBudget her alan/dönem/kategori için tek kayıt; sıfır dahil iki ondalık plan. Her oluşturma/değişimde immutable BudgetRevision, version ve UUID/fingerprint. Bütçe gerçekleşmiş gider değildir.
- Aylık özet, iptal edilmemiş Payment.paidDate tahsilatı ve Expense.occurredDate giderini karşılaştırır. Tahakkuk gelir sayılmaz. Kategori bazında bütçe/gerçekleşen/fark; toplam plan eksi gider ve tahsilat eksi gider ayrı. Eksik bütçe açıkça plansızdır.
- Gider listesi arama/kategori/dönem/durum/sıralama/sayfalama; liste özeti tüm filtre kapsamı, aylık özet seçili dönemin tamamı (arama/kategori filtresinden bağımsız, ekranda açıklanır).
- Dört finans rolü + aktif üyelik / global SUPER_ADMIN. Kişi yetkisi açılmaz. Property kilidi/parent version, BaseEntity sekiz alan, composite FK/check/index, geçmişi koruyan Down; kaynak filtreleri korunur.
- Mevcut shadcn/TanStack RecordList, sağ ManagementPanel, yetkili API, hata katalog/8 saniye bildirim, TR/EN, decimal string/BigInt yeniden kullanılır. Yeni paket yok.

## Resmi görevler

1. Gider modeli/kategoriler ve EF migration.
2. Gider yönetim ekranları: ekleme, liste/filtre, detay/gerekçeli iptal.
3. Aylık gelir–gider/bütçe özeti ve revizyon geçmişli bütçe formu.
4. Finans/yetki/eşzamanlılık/şema, FE ve tarayıcı doğrulaması; takip teslimi.

Kabul senaryoları [TESTING.md](../TESTING.md), durum [STATUS.md](../progress/STATUS.md). Yalnız doğrulanan görev yol haritasında tamamlandı olur.
