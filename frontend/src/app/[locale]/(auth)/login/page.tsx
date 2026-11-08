import { getTranslations } from "next-intl/server";

import { AuthCard } from "@/features/auth/components/auth-card";
import { LoginForm } from "@/features/auth/components/login-form";
import { ROUTES, ROUTE_PARAMS } from "@/constants/routes";
import { Link } from "@/i18n/navigation";

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | undefined>>;
}) {
  const t = await getTranslations("auth.login");
  const params = await searchParams;

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
      <LoginForm returnUrl={params[ROUTE_PARAMS.returnUrl] ?? null} />
    </AuthCard>
  );
}
