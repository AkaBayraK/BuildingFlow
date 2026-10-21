import {headers} from "next/headers";
import {ThemeProvider} from "@/features/theme/ThemeProvider";
import {themeBootstrap} from "@/features/theme/theme";
import type { Metadata } from "next";
import "./globals.css";
import {NextIntlClientProvider} from "next-intl";
import {getLocale,getTranslations} from "next-intl/server";
import {NotificationProvider} from "@/components/feedback/Notifications";
export async function generateMetadata():Promise<Metadata>{const t=await getTranslations("ui");return {title:"BuildingFlow",description:t("metadataDescription")};}
export default async function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  const nonce=(await headers()).get("x-nonce")??undefined;
  return (
    <html lang={await getLocale()} suppressHydrationWarning>
      <head><script nonce={nonce} dangerouslySetInnerHTML={{__html:themeBootstrap}}/></head>
      <body><ThemeProvider><NextIntlClientProvider><NotificationProvider>{children}</NotificationProvider></NextIntlClientProvider></ThemeProvider></body>
    </html>
  );
}
