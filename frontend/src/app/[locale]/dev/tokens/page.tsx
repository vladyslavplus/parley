import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getTranslations } from "next-intl/server";

import { isAppLocale } from "@/i18n/routing";
import { TokensClient } from "./tokens-client";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations({
    locale,
    namespace: "dev.tokens",
  });
  return { title: t("title") };
}

export default async function DevTokensPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();

  if (process.env.NODE_ENV === "production") {
    notFound();
  }

  return <TokensClient />;
}
