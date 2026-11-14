import { ImageResponse } from "next/og";
import { getTranslations } from "next-intl/server";

import { LogoArt } from "@/components/brand/logo-art";
import { APP_NAME } from "@/constants/app";
import { isAppLocale, DEFAULT_LOCALE, LOCALES } from "@/i18n/routing";
import { loadDarkPalette, logoPaintOf } from "@/features/landing/lib/og-tokens";
import {
  SETTLED_WAVE,
  layoutParticles,
  particleLight,
} from "@/features/landing/lib/particles";

export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

export function generateStaticParams() {
  return LOCALES.map((locale) => ({ locale }));
}

/** The alt text belongs to the locale of the page the image is shared from. */
export async function generateImageMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  const t = await getTranslations({
    locale: isAppLocale(locale) ? locale : DEFAULT_LOCALE,
    namespace: "landing.meta",
  });
  return [{ id: "landing", alt: t("ogAlt"), size, contentType }];
}

const FIELD_WIDTH = 1040;
const FIELD_HEIGHT = 132;
const DOT = 7;
const LOGO_SIZE = 64;

/** The same field the landing page draws, settled: a strip under the copy. */
function field() {
  return layoutParticles(110, FIELD_WIDTH / FIELD_HEIGHT).map((particle) => {
    const { lit } = particleLight(particle, SETTLED_WAVE);
    return {
      x: Math.round(particle.x * FIELD_WIDTH),
      y: Math.round(particle.y * FIELD_HEIGHT),
      lit: !particle.holdout && lit > 0,
      holdout: particle.holdout,
    };
  });
}

export default async function OpengraphImage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  const t = await getTranslations({
    locale: isAppLocale(locale) ? locale : DEFAULT_LOCALE,
    namespace: "landing.hero",
  });
  const palette = loadDarkPalette();

  return new ImageResponse(
    <div
      style={{
        width: "100%",
        height: "100%",
        display: "flex",
        flexDirection: "column",
        justifyContent: "space-between",
        backgroundColor: palette.background,
        color: palette.foreground,
        padding: "64px 80px",
      }}
    >
      <div style={{ display: "flex", alignItems: "center", gap: 20 }}>
        {LogoArt({
          tier: "full",
          idPrefix: "og-logo",
          paint: logoPaintOf(palette),
          width: LOGO_SIZE,
          height: LOGO_SIZE,
        })}
        <div
          style={{ display: "flex", fontSize: 44, letterSpacing: "-0.01em" }}
        >
          {APP_NAME}
        </div>
      </div>

      <div
        style={{
          display: "flex",
          maxWidth: 960,
          fontSize: 62,
          lineHeight: 1.1,
          letterSpacing: "-0.02em",
        }}
      >
        {t("title")}
      </div>

      <div
        style={{
          display: "flex",
          position: "relative",
          width: FIELD_WIDTH,
          height: FIELD_HEIGHT,
          borderTop: `1px solid ${palette.border}`,
          paddingTop: 24,
        }}
      >
        <div
          style={{
            display: "flex",
            position: "relative",
            width: FIELD_WIDTH,
            height: FIELD_HEIGHT,
          }}
        >
          {field().map((dot, index) => (
            <div
              key={index}
              style={{
                position: "absolute",
                left: dot.x,
                top: dot.y,
                display: "flex",
                width: DOT,
                height: DOT,
                borderRadius: 999,
                backgroundColor: dot.lit
                  ? palette.highlight
                  : palette["muted-foreground"],
                opacity: dot.lit ? 1 : 0.45,
                ...(dot.holdout
                  ? { border: `1.5px solid ${palette["muted-foreground"]}` }
                  : {}),
              }}
            />
          ))}
        </div>
      </div>
    </div>,
    { ...size },
  );
}
