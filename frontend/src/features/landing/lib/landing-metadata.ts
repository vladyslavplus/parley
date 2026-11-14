import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";

import { APP_NAME } from "@/constants/app";
import { ROUTES } from "@/constants/routes";
import { DEFAULT_LOCALE, LOCALES, type AppLocale } from "@/i18n/routing";

const OPEN_GRAPH_LOCALE: Record<AppLocale, string> = {
  uk: "uk_UA",
  en: "en_US",
};

export const PUBLIC_PAGES = {
  home: "",
  privacy: ROUTES.privacy,
  terms: ROUTES.terms,
} as const;

export type PublicPage = keyof typeof PUBLIC_PAGES;

/** Absolute site origin for canonical and Open Graph URLs, when configured. */
function siteOrigin(): URL | undefined {
  const value = process.env.SITE_URL;
  return value ? new URL(value) : undefined;
}

/** Canonical path plus hreflang alternates for every locale. */
export function localeAlternates(locale: AppLocale, page: PublicPage) {
  const path = PUBLIC_PAGES[page];
  const languages: Record<string, string> = Object.fromEntries(
    LOCALES.map((each) => [each, `/${each}${path}`]),
  );
  languages["x-default"] = `/${DEFAULT_LOCALE}${path}`;
  return { canonical: `/${locale}${path}`, languages };
}

export async function publicPageMetadata(
  locale: AppLocale,
  page: PublicPage,
): Promise<Metadata> {
  const t = await getTranslations({ locale, namespace: "landing" });
  const title =
    page === "home"
      ? { absolute: t("meta.title") }
      : t(`legal.${page}.metaTitle`);
  const description = t("meta.description");

  return {
    metadataBase: siteOrigin(),
    title,
    description,
    alternates: localeAlternates(locale, page),
    openGraph: {
      type: "website",
      siteName: APP_NAME,
      locale: OPEN_GRAPH_LOCALE[locale],
      alternateLocale: LOCALES.filter((each) => each !== locale).map(
        (each) => OPEN_GRAPH_LOCALE[each],
      ),
      url: `/${locale}${PUBLIC_PAGES[page]}`,
      title: page === "home" ? t("meta.title") : t(`legal.${page}.metaTitle`),
      description,
    },
    twitter: { card: "summary", title: t("meta.title"), description },
  };
}
