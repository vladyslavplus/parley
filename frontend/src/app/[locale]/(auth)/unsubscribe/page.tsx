import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";

import { ROUTE_PARAMS } from "@/constants/routes";
import { AuthCard } from "@/features/auth/components/auth-card";
import { UnsubscribePanel } from "@/features/campaigns/components/unsubscribe-panel";
import { isAppLocale } from "@/i18n/routing";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations({
    locale,
    namespace: "campaigns.unsubscribe",
  });
  // The link carries a credential: keep it out of referrers and indexes.
  return {
    title: t("title"),
    referrer: "no-referrer",
    robots: { index: false, follow: false },
  };
}

export default async function UnsubscribePage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<Record<string, string | undefined>>;
}) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations("campaigns.unsubscribe");
  const search = await searchParams;
  const token = search[ROUTE_PARAMS.token] ?? null;

  return (
    <AuthCard title={t("title")} description={t("description")}>
      <UnsubscribePanel token={token} />
    </AuthCard>
  );
}
