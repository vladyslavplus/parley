"use client";

import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { API_ROUTES } from "@/constants/routes";
import { bffFetch } from "@/lib/api/bff-client";
import { describeError, toApiError } from "@/lib/errors";

export function ResendConfirmation({ email }: { email: string }) {
  const t = useTranslations("auth.resend");
  const tKinds = useTranslations("errors.kinds");
  const locale = useLocale();
  const [state, setState] = useState<"idle" | "pending" | "sent">("idle");
  const [error, setError] = useState<string | null>(null);

  async function resend() {
    setState("pending");
    setError(null);
    try {
      await bffFetch(API_ROUTES.resendConfirmation, {
        method: "POST",
        body: { email },
        locale,
      });
      setState("sent");
    } catch (caught) {
      setState("idle");
      setError(describeError(toApiError(caught), (kind) => tKinds(kind)));
    }
  }

  return (
    <div className="flex flex-col gap-3">
      {error ? <Alert>{error}</Alert> : null}
      {state === "sent" ? <Alert tone="info">{t("sent")}</Alert> : null}
      <Button
        variant="secondary"
        onClick={resend}
        disabled={state === "pending"}
      >
        {t("action")}
      </Button>
    </div>
  );
}
