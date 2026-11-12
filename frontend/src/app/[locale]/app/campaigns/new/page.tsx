import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";

import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";
import { CampaignBuilder } from "@/features/campaigns/components/campaign-builder";
import { RequirePermission } from "@/features/session/components/require-permission";
import { isAppLocale } from "@/i18n/routing";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations({ locale, namespace: "campaigns.builder" });
  return { title: t("title") };
}

export default async function NewCampaignPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  return (
    <RequirePermission
      permission={PERMISSIONS.campaignsManage}
      redirectTo={ROUTES.campaigns}
    >
      <CampaignBuilder />
    </RequirePermission>
  );
}
