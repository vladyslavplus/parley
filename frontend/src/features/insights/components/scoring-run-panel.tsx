"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { queryKeys } from "@/constants/query-keys";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import {
  useScoringRun,
  useStartScoringRun,
} from "@/features/insights/hooks/use-scoring";
import {
  isScoringRunFinished,
  runDisabledReason,
  scoringErrorKey,
} from "@/features/insights/lib/scoring";
import type { ScoringModel } from "@/features/insights/types";
import { toApiError } from "@/lib/errors";
import { formatNumber } from "@/lib/formatters";
import { showErrorToast, showSuccessToast } from "@/lib/toasts";

type ScoreKind = "uplift" | "clv";
const KINDS: readonly ScoreKind[] = ["uplift", "clv"];

// Starts a scoring run per kind and follows it until it finishes. A failed run
// leaves the previous scores untouched, and the message says so.
export function ScoringRunPanel({
  mlStatus,
  models,
}: {
  mlStatus: string | undefined;
  models: readonly ScoringModel[];
}) {
  const tScoring = useTranslations("insights.scoring");
  const errorText = useErrorText();
  const queryClient = useQueryClient();
  const start = useStartScoringRun();
  const [runId, setRunId] = useState<string | null>(null);
  const run = useScoringRun(runId);
  const current = run.data;
  const finished = isScoringRunFinished(current?.status);
  const failureKey = scoringErrorKey(current?.errorCode);
  const failedText = failureKey ? tScoring(failureKey) : tScoring("run.failed");
  const busy = start.isPending || (runId !== null && !finished && !run.isError);
  const announced = useRef<string | null>(null);

  // When a run finishes, the ranking and the scores it changed are refreshed, and
  // the outcome is announced once per run (the ref guards against re-runs).
  useEffect(() => {
    if (!current || !isScoringRunFinished(current.status)) return;
    const stamp = `${current.id}:${current.status}`;
    if (announced.current === stamp) return;
    announced.current = stamp;
    void queryClient.invalidateQueries({
      queryKey: [...queryKeys.all, "insights"],
    });
    void queryClient.invalidateQueries({
      queryKey: [...queryKeys.all, "contacts", "scores"],
    });
    if (current.status === "completed") {
      showSuccessToast(
        tScoring("run.completed", {
          scored: formatNumber(Number(current.contactsScored)),
          rejected: formatNumber(Number(current.contactsRejected)),
        }),
      );
    } else {
      const key = scoringErrorKey(current.errorCode);
      showErrorToast(key ? tScoring(key) : tScoring("run.failed"));
    }
  }, [current, queryClient, tScoring]);

  function startError(error: unknown): string {
    const key = scoringErrorKey(toApiError(error).code);
    return key ? tScoring(key) : errorText(error);
  }

  function startRun(kind: ScoreKind) {
    start.mutate(kind, {
      onSuccess: (created) => {
        setRunId(created.id);
        showSuccessToast(tScoring("run.started"));
      },
      onError: (error) => showErrorToast(startError(error)),
    });
  }

  return (
    <section
      aria-labelledby="scoring-run-title"
      className="space-y-4 rounded-[var(--radius-card)] border border-border bg-card p-4"
    >
      <h2
        id="scoring-run-title"
        className="text-sm font-semibold text-foreground"
      >
        {tScoring("run.title")}
      </h2>

      <div className="flex flex-wrap gap-4">
        {KINDS.map((kind) => {
          const reason = runDisabledReason({
            mlStatus,
            hasActiveModel: models.some((model) => model.kind === kind),
            running: busy,
          });
          return (
            <div key={kind} className="space-y-1">
              <Button
                type="button"
                variant="secondary"
                className="px-3 text-xs"
                disabled={reason !== null}
                onClick={() => startRun(kind)}
              >
                {kind === "uplift"
                  ? tScoring("run.uplift")
                  : tScoring("run.clv")}
              </Button>
              {reason === "mlOff" ? (
                <p className="text-[11px] text-muted-foreground">
                  {tScoring("run.disabledMlOff")}
                </p>
              ) : null}
              {reason === "noModel" ? (
                <p className="text-[11px] text-muted-foreground">
                  {tScoring("run.disabledNoModel")}
                </p>
              ) : null}
            </div>
          );
        })}
      </div>

      {runId !== null && current && !finished ? (
        <p
          className="text-xs text-muted-foreground"
          role="status"
          aria-live="polite"
        >
          {current.status === "queued"
            ? tScoring("run.queued")
            : tScoring("run.running")}
          {" · "}
          {tScoring("run.progress", {
            scored: formatNumber(Number(current.contactsScored)),
            total: formatNumber(Number(current.contactsTotal)),
            rejected: formatNumber(Number(current.contactsRejected)),
          })}
        </p>
      ) : null}

      {finished && current?.status === "failed" ? (
        <Alert tone="error">{failedText}</Alert>
      ) : null}
      {run.isError ? <Alert tone="error">{errorText(run.error)}</Alert> : null}
    </section>
  );
}
