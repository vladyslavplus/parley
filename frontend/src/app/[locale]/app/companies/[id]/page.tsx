import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";
import { ViewTransition } from "react";

import { VIEW_TRANSITION_CLASSES } from "@/constants/view-transitions";
import { CompanyRecordView } from "@/features/crm/components/company-record-view";
import { isAppLocale } from "@/i18n/routing";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string; id: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations({
    locale,
    namespace: "crm.companies",
  });
  return { title: t("recordTitle") };
}

export default async function CompanyDetailPage({
  params,
}: {
  params: Promise<{ locale: string; id: string }>;
}) {
  const { locale, id } = await params;
  if (!isAppLocale(locale)) notFound();
  return (
    <ViewTransition enter={VIEW_TRANSITION_CLASSES.revealEnter} default="none">
      <CompanyRecordView id={id} />
    </ViewTransition>
  );
}
