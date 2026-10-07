import type { Metadata } from "next";
import "./globals.css";
export const metadata: Metadata = {
  title: "BuildingFlow",
  description: "Site ve apartman yönetimi",
};
export default function RootLayout({
  children,
}: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="tr">
      <body>{children}</body>
    </html>
  );
}
