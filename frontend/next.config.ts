import createNextIntlPlugin from "next-intl/plugin";
import type { NextConfig } from "next";

const withNextIntl = createNextIntlPlugin("./src/i18n/request.ts");

const nextConfig: NextConfig = {
  // The container image runs the standalone server (`node server.js`), which copies only the files it needs.
  output: "standalone",
  reactCompiler: true,
  experimental: {
    globalNotFound: true,
  },
};

export default withNextIntl(nextConfig);
