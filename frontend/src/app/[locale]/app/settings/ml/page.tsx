import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";

import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";
import { MlSettingsPanel } from "@/features/insights/components/ml-settings-panel";
import { RequirePermission } from "@/features/session/components/require-permission";
import { isAppLocale } from "@/i18n/routing";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations({ locale, namespace: "insights.ml" });
  return { title: t("title") };
}

export default async function SettingsMlPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  return (
    <RequirePermission
      permission={PERMISSIONS.insightsMlManage}
      redirectTo={ROUTES.settingsProfile}
    >
      <MlSettingsPanel />
    </RequirePermission>
  );
}
