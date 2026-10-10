import type { NextConfig } from "next";
import createNextIntlPlugin from "next-intl/plugin";
const config: NextConfig = { output: "standalone" };
export default createNextIntlPlugin()(config);
