import { getTranslations } from "next-intl/server";

import { AuthCard } from "@/features/auth/components/auth-card";
import { ResendConfirmation } from "@/features/auth/components/resend-confirmation";
import { ROUTES, ROUTE_PARAMS } from "@/constants/routes";
import { Link } from "@/i18n/navigation";

export default async function CheckEmailPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | undefined>>;
}) {
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
