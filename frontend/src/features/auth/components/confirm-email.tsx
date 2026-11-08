"use client";

import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { API_ROUTES, ROUTES } from "@/constants/routes";
import { Link } from "@/i18n/navigation";
import { bffFetch } from "@/lib/api/bff-client";
import { describeError, toApiError } from "@/lib/errors";

export function ConfirmEmail({
  userId,
  token,
}: {
  userId: string;
  token: string;
}) {
  const t = useTranslations("auth.confirm");
  const tKinds = useTranslations("errors.kinds");
  const locale = useLocale();
  const [state, setState] = useState<"idle" | "pending" | "done">("idle");
  const [error, setError] = useState<string | null>(null);

  async function confirm() {
    setState("pending");
    setError(null);
    try {
      await bffFetch(API_ROUTES.confirmEmail, {
        method: "POST",
        body: { userId, token },
        locale,
      });
      setState("done");
    } catch (caught) {
      setState("idle");
      setError(describeError(toApiError(caught), (kind) => tKinds(kind)));
    }
  }

  if (state === "done") {
    return (
      <div className="flex flex-col gap-3">
        <Alert tone="info">{t("success")}</Alert>
        <Link href={ROUTES.login} className="text-sm underline">
          {t("toLogin")}
        </Link>
      </div>
    );
  }
  return (
    <div className="flex flex-col gap-3">
      {error ? <Alert>{error}</Alert> : null}
      <Button onClick={confirm} disabled={state === "pending"} autoFocus>
        {t("action")}
      </Button>
    </div>
  );
}
