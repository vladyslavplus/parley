import { getTranslations } from "next-intl/server";

import { AcceptInvitationForm } from "@/features/auth/components/accept-invitation-form";
import { AuthCard } from "@/features/auth/components/auth-card";
import { Alert } from "@/components/ui/alert";
import { ROUTE_PARAMS } from "@/constants/routes";

export default async function AcceptInvitationPage({
  searchParams,
}: {
  searchParams: Promise<Record<string, string | undefined>>;
}) {
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
