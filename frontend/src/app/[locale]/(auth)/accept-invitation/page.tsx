import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";

import { AcceptInvitationForm } from "@/features/auth/components/accept-invitation-form";
import { AuthCard } from "@/features/auth/components/auth-card";
import { Alert } from "@/components/ui/alert";
import { ROUTE_PARAMS } from "@/constants/routes";
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
    namespace: "auth.acceptInvitation",
  });
  return { title: t("title") };
}

export default async function AcceptInvitationPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<Record<string, string | undefined>>;
}) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations("auth.acceptInvitation");
  const token = (await searchParams)[ROUTE_PARAMS.token];

  return (
    <AuthCard title={t("title")} description={t("description")}>
      {token ? (
        <AcceptInvitationForm token={token} />
      ) : (
        <Alert>{t("invalidLink")}</Alert>
      )}
    </AuthCard>
  );
}
