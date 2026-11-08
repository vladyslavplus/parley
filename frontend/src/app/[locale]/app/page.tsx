import { ROUTES } from "@/constants/routes";
import { redirect } from "next/navigation";
import type { AppLocale } from "@/i18n/routing";

export default async function AppIndexPage({
  params,
}: {
  params: Promise<{ locale: AppLocale }>;
}) {
  const { locale } = await params;
  redirect(`/${locale}${ROUTES.settingsProfile}`);
}

export const dynamic = "force-dynamic";
