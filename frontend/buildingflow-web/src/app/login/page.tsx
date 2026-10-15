import {getTranslations} from "next-intl/server";
import {LanguageSwitcher} from "@/components/LanguageSwitcher";
import {BuildingFlowMark} from "@/components/BuildingFlowMark";
import {ArrowRight, Building2, CircleCheck, FileText, ShieldCheck, WalletCards} from "lucide-react";
import Image from "next/image";

export default async function Login() {
  const t=await getTranslations("ui");
  return <main className="signed-out signout-layout">
    <div className="signout-shell">
      <section className="signout-brand-panel" aria-label="BuildingFlow">
        <div className="signout-wordmark"><BuildingFlowMark size={48} variant="light"/><span>Building<span className="signout-flow">Flow</span></span></div>
        <div className="signout-brand-copy">
          <span className="signout-eyebrow">{t("signoutBrandEyebrow")}</span>
          <h2>{t("signoutBrandHeadline")}<span>{t("signoutBrandHeadlineAccent")}</span></h2>
          <p>{t("signoutBrandDescription")}</p>
        </div>
        <div className="signout-city" aria-hidden="true"><Image src="/buildingflow-buildings.svg" width={1100} height={1100} alt="" /></div>
        <div className="signout-feature-list">
          <span><Building2 size={18} aria-hidden="true"/>{t("signoutFeatureProperties")}</span>
          <span><WalletCards size={18} aria-hidden="true"/>{t("signoutFeaturePayments")}</span>
          <span><FileText size={18} aria-hidden="true"/>{t("signoutFeatureDocuments")}</span>
        </div>
      </section>
      <section className="signout-action-panel" aria-labelledby="signout-title">
        <div className="signout-toolbar"><LanguageSwitcher/></div>
        <div className="signout-content">
          <div className="signout-status-icon"><CircleCheck size={32} strokeWidth={1.6} aria-hidden="true"/></div>
          <span className="signout-status-label">{t("signoutStatusLabel")}</span>
          <h1 id="signout-title">{t("youHaveSignedOut")}</h1>
          <p className="signout-description">{t("youCanSignInAgainWithYourBuildingFlowAccount")}</p>
          <a className="login-button signout-login-button" href="/"><span>{t("signoutLoginAgain")}</span><ArrowRight size={19} aria-hidden="true"/></a>
          <div className="signout-security"><ShieldCheck size={18} aria-hidden="true"/><span>{t("signoutSecurityNote")}</span></div>
        </div>
        <div className="signout-footer"><span>BuildingFlow</span><span>{t("signoutFooterTagline")}</span></div>
      </section>
    </div>
  </main>;
}
