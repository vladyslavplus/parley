import type { MetadataRoute } from "next";

import { APP_NAME } from "@/constants/app";
import { loadDarkPalette } from "@/features/landing/lib/og-tokens";

/** The installed app opens on the dark graphite ground of its icon. */
export default function manifest(): MetadataRoute.Manifest {
  const palette = loadDarkPalette();

  return {
    name: APP_NAME,
    short_name: APP_NAME,
    start_url: "/",
    display: "standalone",
    background_color: palette.background,
    theme_color: palette.background,
    icons: [
      { src: "/icon-192.png", sizes: "192x192", type: "image/png" },
      { src: "/icon-512.png", sizes: "512x512", type: "image/png" },
      {
        src: "/icon-maskable-512.png",
        sizes: "512x512",
        type: "image/png",
        purpose: "maskable",
      },
    ],
  };
}
