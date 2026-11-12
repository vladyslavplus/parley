"use client";

import { CheckCircle2, CircleAlert, Clock } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState, type ReactNode } from "react";

import { Button } from "@/components/ui/button";
import { API_ROUTES, ROUTE_PARAMS } from "@/constants/routes";
import {
  unsubscribeFailure,
  type UnsubscribeFailure,
} from "@/features/campaigns/lib/unsubscribe";
import { bffFetch } from "@/lib/api/bff-client";

type State = "confirm" | "pending" | "done" | UnsubscribeFailure;

function Message({
  icon,
  title,
  children,
}: {
  icon: ReactNode;
  title: string;
  children?: ReactNode;
}) {
  return (
    <div className="flex items-start gap-3">
      <span className="mt-0.5 shrink-0">{icon}</span>
      <div className="space-y-1">
        <p className="text-sm font-medium text-foreground">{title}</p>
        {children}
      </div>
    </div>
  );
}

// Anonymous confirm step: the link opens this page, and only the button sends
// the request, so mail scanners that open links cannot unsubscribe anyone.
export function UnsubscribePanel({ token }: { token: string | null }) {
  const t = useTranslations("campaigns.unsubscribe");
  const locale = useLocale();
  const [state, setState] = useState<State>(token ? "confirm" : "invalid");

  async function confirm() {
    if (!token) return;
    setState("pending");
    try {
      await bffFetch(API_ROUTES.unsubscribe, {
        method: "POST",
        query: { [ROUTE_PARAMS.token]: token },
        locale,
      });
      setState("done");
    } catch (error) {
      setState(unsubscribeFailure(error));
    }
  }

  const canConfirm =
    state === "confirm" || state === "pending" || state === "failed";

  return (
    <div
      aria-live="polite"
      className="flex min-h-36 flex-col justify-between gap-4"
    >
      {state === "done" ? (
        <Message
          icon={<CheckCircle2 className="h-5 w-5 text-success" />}
          title={t("done.title")}
        >
          <p className="text-sm text-muted-foreground">{t("done.body")}</p>
        </Message>
      ) : state === "invalid" ? (
        <Message
          icon={<CircleAlert className="h-5 w-5 text-destructive" />}
          title={t("invalid.title")}
        >
          <p className="text-sm text-muted-foreground">{t("invalid.body")}</p>
        </Message>
      ) : state === "rate_limited" ? (
        <Message
          icon={<Clock className="h-5 w-5 text-warning" />}
          title={t("rateLimited.title")}
        >
          <p className="text-sm text-muted-foreground">
            {t("rateLimited.body")}
          </p>
        </Message>
      ) : (
        <p className="text-sm text-muted-foreground">
          {state === "failed" ? t("failed") : t("confirm.body")}
        </p>
      )}

      {canConfirm ? (
        <Button
          type="button"
          variant="primary"
          disabled={state === "pending"}
          onClick={() => void confirm()}
          autoFocus
        >
          {state === "pending" ? t("pending") : t("confirm.action")}
        </Button>
      ) : null}
      {state === "rate_limited" ? (
        <Button
          type="button"
          variant="secondary"
          onClick={() => void confirm()}
        >
          {t("rateLimited.retry")}
        </Button>
      ) : null}
    </div>
  );
}
