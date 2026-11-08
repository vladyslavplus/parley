import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";

import { AuthCard } from "@/features/auth/components/auth-card";
import { LoginForm } from "@/features/auth/components/login-form";
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
    namespace: "auth.login",
  });
  return { title: t("title") };
}

export default async function LoginPage({
  params,
  searchParams,
}: {
  params: Promise<{ locale: string }>;
  searchParams: Promise<Record<string, string | undefined>>;
}) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations("auth.login");
  const search = await searchParams;

  return (
    <AuthCard
      title={t("title")}
      description={t("description")}
      footer={
        <div className="flex flex-col gap-1">
          <Link href={ROUTES.forgotPassword} className="underline">
            {t("forgot")}
          </Link>
          <Link href={ROUTES.register} className="underline">
            {t("toRegister")}
          </Link>
        </div>
      }
    >
      <LoginForm returnUrl={search[ROUTE_PARAMS.returnUrl] ?? null} />
    </AuthCard>
  );
}
