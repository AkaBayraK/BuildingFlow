import type { Metadata } from "next";
import "./globals.css";
import {NextIntlClientProvider} from "next-intl";
import {getLocale,getTranslations} from "next-intl/server";
import {NotificationProvider} from "@/components/feedback/Notifications";
export async function generateMetadata():Promise<Metadata>{const t=await getTranslations("ui");return {title:"BuildingFlow",description:t("metadataDescription")};}
export default async function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang={await getLocale()}>
      <body><NextIntlClientProvider><NotificationProvider>{children}</NotificationProvider></NextIntlClientProvider></body>
    </html>
  );
}
