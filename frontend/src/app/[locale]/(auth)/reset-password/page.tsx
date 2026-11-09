import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";

import { AuthCard } from "@/features/auth/components/auth-card";
import { ResetPasswordForm } from "@/features/auth/components/reset-password-form";
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
    namespace: "auth.reset",
  });
  return { title: t("title") };
}

export default async function ResetPasswordPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<Record<string, string | undefined>>;
}) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations("auth.reset");
  const search = await searchParams;
  const userId = search[ROUTE_PARAMS.userId];
  const token = search[ROUTE_PARAMS.token];

  return (
    <AuthCard title={t("title")} description={t("description")}>
      {userId && token ? (
        <ResetPasswordForm userId={userId} token={token} />
      ) : (
        <Alert>{t("invalidLink")}</Alert>
      )}
    </AuthCard>
  );
}
