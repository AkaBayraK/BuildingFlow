import {SessionWorkspaces} from "@/features/session-workspaces/SessionWorkspaces";
import {getTranslations} from "next-intl/server";
import { Roadmap } from "../../features/roadmap/Roadmap";
import Link from "next/link";
import { getApiHealth } from "../../services/health";
export const dynamic = "force-dynamic";
export default async function Home() {
  if(process.env.BUILDINGFLOW_SESSION_CONTEXTS_ENABLED==="true")return <SessionWorkspaces automatic/>;
  const t=await getTranslations("ui");

  const health = await getApiHealth();
  return (<>
        <section className="intro">
          <span className="eyebrow">{t("aMOREORGANIZEDCOMMUNITY")}</span>
          <h1>
            {t("theFoundationOfEverything")}<br />
            {t("isGoodManagement")}</h1>
          <p>
            {t("welcomeToBuildingFlowManageApartmentsCommunitiesResidencesAndMixed")}</p>
        </section>
        <section className="cards">
          <article>
            <span className="card-label">{t("systemStatus")}</span>
            <div className={`status ${health}`}>
              {health === "healthy"
                ? t("apiAvailable")
                : t("apiUnavailable")}
            </div>
            <p>
              {t("postgresqlConnectivityIsCheckedThroughTheAPIHealthEndpoint")}</p>
            <a href="/health">{t("viewServiceHealth")}</a>
          </article>
          <article>
            <span className="card-label">{t("firstModule")}</span>
            <h2>{t("properties")}</h2>
            <p>
              {t("apartmentUnitsCommunityBuildingBlockUnit")}<br />
              {t("createListEditAndDeleteAreAvailableAddUnits")}</p>
            <Link className="primary-link" href="/properties">{t("openProperties")}</Link>
          </article>
          <article>
            <span className="card-label">{t("identityAndAccess")}</span>
            <h2>{t("keycloakOIDC")}</h2>
            <p>
              {t("secureSignInSignOutAndPasswordResetWith")}</p>
            <span className="tag">{t("signInAndPasswordResetCompleted")}</span>
          </article>
        </section>
        <Roadmap />
  </>);
}
