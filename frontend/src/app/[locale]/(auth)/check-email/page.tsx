import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";

import { AuthCard } from "@/features/auth/components/auth-card";
import { ResendConfirmation } from "@/features/auth/components/resend-confirmation";
import { ROUTES, ROUTE_PARAMS } from "@/constants/routes";
import { isAppLocale } from "@/i18n/routing";
import { Link } from "@/i18n/navigation";

export async function generateMetadata({
  params,
}: {
  params: Promise<{ locale: string }>;
}): Promise<Metadata> {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations({
    locale,
    namespace: "auth.checkEmail",
  });
  return { title: t("title") };
}

export default async function CheckEmailPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<Record<string, string | undefined>>;
}) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations("auth.checkEmail");
  const email = (await searchParams)[ROUTE_PARAMS.email];

  return (
    <AuthCard
      title={t("title")}
      description={
        email ? t("descriptionWithEmail", { email }) : t("description")
      }
      footer={
        <Link href={ROUTES.login} className="underline">
          {t("toLogin")}
        </Link>
      }
    >
      {email ? <ResendConfirmation email={email} /> : null}
    </AuthCard>
  );
}
