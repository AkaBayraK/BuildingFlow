# BuildingFlow public website

Ayrı Next.js/TypeScript uygulaması3004; mevcut API5005 ve yönetim3003 ile çalışır. Yeni bağımlılık/kimlik deposu yoktur; mevcut frontend kilitli Node bağımlılıkları paylaşılır.

```bash
bash scripts/dev.sh website
# veya
npm run setup --prefix website
npm run dev --prefix website
```

Kök demo/noindex; gerçek site `/{siteKey}`. Yetkili taslak önizleme ayrı, public veri sadece aktif yayından okunur. Tek Hesabıma Giriş yeni sekmede açılır; gerçek site site/all bağlamını, demo genel yönetim girişini kullanır. Site içeriği/medya/tema backend CMS'den gelir; parola public sitede alınmaz. No-store/DTO/SHA/Host/DNS/TLS kapıları korunur.

```bash
python3 scripts/check-licenses.py
bash scripts/check-website.sh
docker build -f website/Dockerfile -t buildingflow-website:local .
python3 scripts/test-website-container.py buildingflow-website:local
```

[İşletim/yayın/geri alma](../docs/website-management/operations.md), [yönetici kullanım rehberi](../docs/website-management/admin-guide.md), [test kanıtları](../docs/TESTING.md), [domain](../docs/api/website-domains.md), [giriş](../docs/api/website-login.md), [görsel kaynakları](public/images/README.md).

Yerel teknik kontroller üretim kabulü değildir. EİDS/hukuki, gerçek DNS/TLS, kimlikli UAT, cihaz/ekran okuyucu/performance/OS ve dağıtım kabulü [açık görevde](https://github.com/AkaBayraK/BuildingFlow/issues/30) izlenir.
