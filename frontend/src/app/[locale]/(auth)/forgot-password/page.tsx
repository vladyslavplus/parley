import { getTranslations } from "next-intl/server";

import { AuthCard } from "@/features/auth/components/auth-card";
import { ForgotPasswordForm } from "@/features/auth/components/forgot-password-form";
import { ROUTES } from "@/constants/routes";
import { Link } from "@/i18n/navigation";

export default async function ForgotPasswordPage() {
  const t = await getTranslations("auth.forgot");

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
      <ForgotPasswordForm />
    </AuthCard>
  );
}
