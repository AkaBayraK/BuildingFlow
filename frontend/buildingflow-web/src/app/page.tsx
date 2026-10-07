import Image from "next/image";
import { getApiHealth } from "../services/health";
export const dynamic = "force-dynamic";
export default async function Home() {
  const health = await getApiHealth();
  return (
    <div className="shell">
      <aside>
        <div className="brand">
          <Image
            src="/buildingflow-logo.png"
            alt="BuildingFlow"
            width={174}
            height={174}
            priority
          />
        </div>
        <span className="label">YÖNETİM ALANI</span>
        <nav>
          <a className="active" href="/">
            ◫ <span>Genel bakış</span>
          </a>
        </nav>
        <p className="sidebar-note">
          Siteler · Bloklar · Daireler
          <br />
          <span>Sonraki geliştirme adımı</span>
        </p>
        <div className="sidebar-footer">
          BuildingFlow <span>v0.1 · Temel kurulum</span>
        </div>
      </aside>
      <main>
        <header>
          <span>
            Çalışma alanı / <strong>Genel bakış</strong>
          </span>
          <span className="badge">Geliştirme ortamı</span>
        </header>
        <section className="intro">
          <span className="eyebrow">DAHA DÜZENLİ BİR YAŞAM ALANI</span>
          <h1>
            Her şeyin temeli,
            <br />
            iyi bir yönetim.
          </h1>
          <p>
            BuildingFlow çalışma alanına hoş geldiniz. Site, blok ve daire
            yönetimi için ilk temel hazır.
          </p>
        </section>
        <section className="cards">
          <article>
            <span className="card-label">Sistem durumu</span>
            <div className={`status ${health}`}>
              {health === "healthy"
                ? "● API erişilebilir"
                : "○ API erişilemiyor"}
            </div>
            <p>
              PostgreSQL bağlantısı API sağlık kontrolü üzerinden doğrulanır.
            </p>
            <a href="/health">Sağlık durumunu görüntüle ↗</a>
          </article>
          <article>
            <span className="card-label">İlk modül</span>
            <h2>Yaşam alanları</h2>
            <p>
              Site → Blok → Daire
              <br />
              Domain kuralları ve veri altyapısı hazır. Yönetim ekranları bir
              sonraki adımda eklenecek.
            </p>
            <span className="tag">Temel oluşturuldu</span>
          </article>
          <article>
            <span className="card-label">Kimlik ve erişim</span>
            <h2>Keycloak / OIDC</h2>
            <p>
              JWT doğrulama temeli hazır. Kullanıcı oturumu ve yetki
              politikaları sonraki aşamada tamamlanacak.
            </p>
            <span className="tag">Entegrasyon hazırlığı</span>
          </article>
        </section>
        <section className="next">
          <span>01</span>
          <div>
            <h2>Sıradaki adım: İlk siteyi oluşturmak</h2>
            <p>
              Yetkilendirilmiş bir API üzerinden site ekleme ve listeleme akışı.
            </p>
          </div>
        </section>
        <footer>
          BuildingFlow · Site ve apartman yönetimi{" "}
          <span>Birlikte daha düzenli.</span>
        </footer>
      </main>
    </div>
  );
}
