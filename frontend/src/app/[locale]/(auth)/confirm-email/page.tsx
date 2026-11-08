import { getTranslations } from "next-intl/server";

import { AuthCard } from "@/features/auth/components/auth-card";
import { ConfirmEmail } from "@/features/auth/components/confirm-email";
import { Alert } from "@/components/ui/alert";
import { ROUTE_PARAMS } from "@/constants/routes";

export default async function ConfirmEmailPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | undefined>>;
}) {
  const t = await getTranslations("auth.confirm");
  const params = await searchParams;
  const userId = params[ROUTE_PARAMS.userId];
  const token = params[ROUTE_PARAMS.token];

  return (
    <AuthCard title={t("title")} description={t("description")}>
      {userId && token ? (
        <ConfirmEmail userId={userId} token={token} />
      ) : (
        <Alert>{t("invalidLink")}</Alert>
      )}
    </AuthCard>
  );
}
