import type { Metadata } from "next";
import { getTranslations } from "next-intl/server";
import { notFound } from "next/navigation";

import { AuthCard } from "@/features/auth/components/auth-card";
import { RegisterForm } from "@/features/auth/components/register-form";
import { ROUTES } from "@/constants/routes";
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
    namespace: "auth.register",
  });
  return { title: t("title") };
}

export default async function RegisterPage({
  params,
}: {
  params: Promise<{ locale: string }>;
}) {
  const { locale } = await params;
  if (!isAppLocale(locale)) notFound();
  const t = await getTranslations("auth.register");

  return (
    <AuthCard
      title={t("title")}
      description={t("description")}
      footer={
        <Link href={ROUTES.login} className="underline">
          {t("toLogin")}
        </Link>
      }
    >
      <RegisterForm />
    </AuthCard>
  );
}
