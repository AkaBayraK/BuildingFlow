# BuildingFlow Next.js çalışma kuralları

Bu klasördeki her kodlama/refactor öncesinde [kök AGENTS.md](../../AGENTS.md) ve **[Frontend Convention](../../docs/frontend-rules.md)** okunur. Ayrıntılı kuralların tek kaynağı frontend-rules.md; aşağıdaki maddeler en önemli çalışma kurallarıdır. Kullanıcının güncel talebi önceliklidir.

- Mevcut shadcn/ui + Tailwind + TanStack Table, feature modülleri, EntityEditor/HierarchyRows ve merkezi `services/api.ts` davranışlarını tekrar kullan. Ekran başına yeni form/modal/API sistemi yazma.
- 04'te eklenen RecordList (masaüstü tablo/mobil kart), lib/format.ts date-only formatter ve people rota dönüş testlerini tekrar kullan. Kişisel veri API'si yalnızca yönetici rolüne açık; Person giriş hesabı değildir. Tarih input değişimini diğer alan değişimi ve kaydedilen değerle birlikte doğrula; iki uç dahil bitiş günü hâlâ geçerlidir.
- i18n, form, API, permission, tema, notification, state, responsive ve formatting için Convention'ın 17 başlığını uygula. Kurulu olmayan RHF/Zod/TanStack Query/Zustand veya lint/hook altyapısını hazır varsayma; durum tablosunu kontrol et. İlgili ortak altyapı geçişini mevcut akışı koruyarak yap, bu belgeyi okumak için bütün paketleri kurma.
- Kullanıcı metinleri/erişilebilir adlar hedef TR/EN sözlük anahtarlarından, renkler semantic token'dan, sayısal/tarihsel gösterimler ortak locale formatter'dan gelmeli. Mevcut dağınık Türkçe/renk/format kullanımını yeni bağımsız kalıplarla çoğaltma.
- Liste/form loading, empty, error, başarı, iptal, gönderim kilidi ve version/409 kurtarma davranışlarını kapsar. Soft-delete mesajı geçmişin saklandığını söyler. Dolu/pasif üst kayıt ve tür değişimindeki ilişki/geçmiş kurallarını backend doğrular.
- Property → opsiyonel Building → Unit; apartmanda sahte blok yok. Canonical properties/buildings/units route'ları ve OIDC dönüş whitelist'i korunur. Yeni locale/route/query state oturum ve eski bağlantı testleriyle birlikte gelir.
- Apartman/bina/daire detay alanlarında `features/property-details` ortak tanımlarını ve nullable değer korumasını kullan. PUT/409 reload mevcut detayları taşımalı; false/0/null ayrımı, net≤brüt ve enum/kat uyumu korunur. Ayrıntı ve kabul kanıtı ortak Convention ile docs/api/hierarchy.md'dedir.
- API çağrısı, token refresh ve HTTP hata eşlemesi ortak katmanda; business component'e raw fetch/token deposu ekleme. Mevcut server-only health ve resmi Keycloak adapter'ı ayrı sorumluluklardır. Mutation için kör retry/idempotency varsayımı yok.
- Backend rol + aktif üyelik son otoritedir. `Can`/permission hedefi token'a hayali yetki eklemez; mevcut SITE_ADMIN dahil rolleri sessizce değiştirme. Hassas veriyi anonim SSR/ortak cache'e koyma; logout/kullanıcı değişiminde iş verisi temizlenir.
- Strict TypeScript korunur; unknown dış veri doğrulanır. Token/parola/reset linki/auth callback veya kişisel veri loglanmaz; gizli değerler NEXT_PUBLIC değişkenlerine, tarayıcıya açık yapılandırmaya veya client bundle'a taşınmaz.
- Senaryoları önce docs/TESTING.md'ye yaz; frontend kod/config değişince typecheck/build ve ilgili kabul testlerini çalıştır. Paket kurulum/restore sonrası lisans kontrolü; bilinmeyen lisans, ücretli/lisanssız template/asset yok. Gerçek sonuçlar ile roadmap/skill-agent kaydı güncellenir.

- next-intl TR/EN, LanguageSwitcher/cookie, MessageBanner/FieldError ve useNotification artık uygulanmıştır. Yeni metni messages/tr.json ve en.json eş anahtarlara ekle; form/alert/ARIA/formatter locale'e uyar. `npm test` sözlük, ICU parametreleri, API fallback, format ve OIDC dönüş sözleşmesini kontrol eder.
- Daireler ve yetkili kullanıcı için Kişiler menüleri genel bakış/liste dahil her zaman görünür. Seçili alan yokken `/units` ve `/people` erişilebilen yönetim alanını seçtirir; seçim aynı modülün alan içindeki sayfasına gider. PropertyScope seçili alanda bağlantıları o alana bağlar: genel bilgiler, bina/blok, daire/bölüm, kişiler. Apartmanda blok menüsü yok; kişi ilişkileri ayrı person sayfasıdır. Yeni route eklerken auth/return-path.ts whitelist ve testlerini güncelle.

<!-- BEGIN:nextjs-agent-rules -->

## This is NOT the Next.js you know

This version has breaking changes — APIs, conventions, and file structure may all differ from your training data. Read the relevant guide in `node_modules/next/dist/docs/` (resolved from this file's directory; in monorepos the `next` package may not be visible from the repo root) before writing any code. Heed deprecation notices.

This block is written and re-added by `next dev` — verify at `node_modules/next/dist/server/lib/generate-agent-files.js`. Removing it from a diff only re-creates the uncommitted change; committing it with your work keeps the tree clean.

<!-- END:nextjs-agent-rules -->

- 05 Aidat / Borçlar ayrı kalıcı modüldür: `/dues` alan seçimi, `/properties/{id}/dues` liste/özet/tahakkuk/iptal. Muhasebeci finansı görür; Kişiler yönetici rolünde kalır. Para API’de string, önizleme kuruş üzerinden hesaplanır; decimal hassasiyeti korunur. Bulk seçim 1–500, belirsiz sonuçta aynı form içeriğine aynı UUID; kör mutation retry yok. Yeni finance DTO’ları runtime shape/property scope kontrolünden geçer. TR/EN metin, ikonlu feedback, RecordList mobil/masaüstü ve OIDC whitelist/sözleşme testleri tekrar kullanılır.

- UI 06: yeni yönetim listeleri WorkspacePresentation + RecordList compact/desktop/mobile kalıbını kullanır. API kullanan detay akışı features/hierarchy/RecordDetails'te; sunum bileşenlerine fetch taşıma. ManagementPanel ile bütün yönetim oluşturma/düzenleme/detay ve ilişki bitiş formları sağdan açılır; merkezde silme/onay korunur; kapanış odağını ve busy kilidini test et. Aktif/pasif doluluk değildir; Person listesine tek OWNER/TENANT rolü uydurma. Dış Unit/Person/Occupancy DTO'larını management-contracts ile scope/shape kontrolünden geçir; veriyi anonim SSR'a koyma. Görsel UI06 ve iş fazı 06 ödeme/tahsilat ayrı kayıttır.

- 06 Tahsilatlar ayrı kalıcı finans modülüdür: `/payments` → `/properties/{id}/payments`; OIDC whitelist korunur. PaymentEditor/PaymentDetail ortak ManagementPanel, liste RecordList, borç geçmişi feature katmanındadır. Para string/BigInt kuruş; seçilmiş 1–100 borcun version'ı ve tam toplam gönderilir. Belirsiz gönderimde aynı içerik UUID'si korunur, cancelled replay yeni ödeme diye gösterilmez. CURRENT receipt GET/scope/history şekli doğrulanır; gerçek para transferi veya fiskal makbuz iddiası ekleme. Accounting rolü kişisel veri menüsüne erişmez.

- Tahakkuk bulk seçimi: bütün sonuçlar ile görünür sayfa ayrı eylemlerdir. Filtre dışı mevcut seçimleri/dedup/500 limiti koru; eksik/bozuk/iptal edilen paginated read'i kısmi başarı diye uygulama. Seçim state'i borç kaydı değildir; backend'in tek atomik POST/version/idempotency/yetki/dönem tekilliğini koru.

- 06.1: Borç kişi/rol bilgileri güncel kişilerden değil DuesCharge.parties snapshot'ından; legacy/role-restricted/recorded-empty durumlarını ayırt et. Kişi combo yalnız property-scope dues/person-choices kaynağı, arama/sayfalama/runtime DTO ile çalışır. ACCOUNTANT kişi filtresi/kolonu/kişisel muhatap seçimini görmez; backend de reddeder. İptal sebep select'i katalogdan, OTHER açıklamalı. Kiracı bitişinde önizleme ve onay varsayılan kapalı; tarih/version/refresh değişince onayı sil. Yalnız listelenmiş ID/version onayı gönder; belirsiz/stale sonuçta kör retry yok. Aynı sağ ManagementPanel, feedback ve TR/EN formatı kullanılır.

- 07.1 için merkezi ErrorCatalog / services/error-codes.ts ve apiErrors/apiFields sözlüğünü kullan; ham API message/title basma. Genel error/warning sağ üst 8 saniyelik ortak bildirime gider; field açıklamaları ve retry/409 eylemleri korunur. Yeni kod/parametre, TR/EN ve abort/negatif sözleşme testi birlikte eklenir. Ayrıntı ortak frontend-rules.md ve docs/api/errors.md.

- 07 Gider ve Bütçe kalıcı finans menüsü: `/expenses` alan seçimi, `/properties/{id}/expenses` liste/aylık özet/kategori planı. Mevcut RecordList ve sağ ManagementPanel; tutar string/BigInt, plansız kategori ile sıfır plan ayrımı, bağımsız filtre/ay özetleri, değişmez revizyon ve 409 güncel bütçe akışı. Yeni form metin/alan/ARIA ve kodlar TR/EN kaynaklarına eklenir; ham hata yok. Finans kayıtlarını silme/düzenleyerek geçmişi kaybettirme.

- Daire–kişi bağlantısında `features/people/unit-links.ts` ve `UnitPeople` kalıbını kullan: sadece yönetici rolünde görünen sayfa batch'i; güncel OWNER/TENANT, hatayı boş ilişki gibi gösterme. Kişiden bölüme tam UUID+Property/Building filtreli canonical route, strict DTO ve güvenli OIDC query whitelist'i korunur. Tablet kart/44px bağlantı kalıbı ve test kanıtı frontend-rules.md / TESTING.md'dedir.
