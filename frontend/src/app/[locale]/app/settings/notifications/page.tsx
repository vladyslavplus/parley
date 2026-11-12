import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";

import { NotificationPreferencesPanel } from "@/features/notifications/components/notification-preferences-panel";
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
    namespace: "notifications.preferences",
  });
  return { title: t("title") };
}

// Every signed-in member can change their own notification settings.
export default async function SettingsNotificationsPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  return <NotificationPreferencesPanel />;
}
