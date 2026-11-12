import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";

import { RequirePermission } from "@/features/session/components/require-permission";
import { ModelRegistryView } from "@/features/insights/components/model-registry-view";
import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";
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
    namespace: "insights.scoring.registry",
  });
  return { title: t("title") };
}

// Scoring models and their provenance. Viewing needs `insights.ml.view`; the run
// controls on the page appear only with `insights.ml.run`.
export default async function SettingsMlModelsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  return (
    <RequirePermission
      permission={PERMISSIONS.insightsMlView}
      redirectTo={ROUTES.settingsProfile}
    >
      <ModelRegistryView />
    </RequirePermission>
  );
}
