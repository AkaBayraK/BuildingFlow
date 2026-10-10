import {getTranslations} from "next-intl/server";
import {LanguageSwitcher} from "@/components/LanguageSwitcher";
import Image from "next/image";
export default async function Login() {
  const t=await getTranslations("ui");

  return <main className="signed-out"><LanguageSwitcher/><Image src="/buildingflow-logo.png" width={180} height={180} alt="BuildingFlow"/><h1>{t("youHaveSignedOut")}</h1><p>{t("youCanSignInAgainWithYourBuildingFlowAccount")}</p><a className="login-button" href="/">{t("signIn")}</a></main>;
}
