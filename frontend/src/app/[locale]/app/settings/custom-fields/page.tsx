import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";

import { CustomFieldsSettingsPanel } from "@/features/crm/components/custom-fields-settings-panel";
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
    namespace: "settings.customFields",
  });
  return { title: t("title") };
}

export default async function SettingsCustomFieldsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations("settings.customFields");

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">{t("title")}</h1>
      <CustomFieldsSettingsPanel />
    </section>
  );
}
