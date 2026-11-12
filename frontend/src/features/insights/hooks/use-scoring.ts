"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { queryKeys } from "@/constants/query-keys";
import { insightsJson } from "@/features/insights/lib/insights-api";
import {
  newestModelOf,
  scoringRunPollInterval,
} from "@/features/insights/lib/scoring";
import type {
  ContactScores,
  PriorityPage,
  ScoringModel,
  ScoringRun,
} from "@/features/insights/types";

// Contact scores for the record panel. Values come back only for viewers with
// `insights.ml.view`; the panel handles that.
export function useContactScores(contactId: string, enabled: boolean) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.contactScores(contactId),
    queryFn: () =>
      insightsJson<ContactScores>(
        `/contacts/${encodeURIComponent(contactId)}/scores`,
        {
          locale,
        },
      ),
    enabled,
  });
}

export function useScoringModels(enabled: boolean) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.insightsModels(),
    queryFn: () => insightsJson<ScoringModel[]>("/insights/models", { locale }),
    enabled,
  });
}

// The ML status of the priority list, read from its first row. The list also
// says whether it is ranked by ML or RFM; the status explains why.
export function useMlStatus(enabled: boolean) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.insightsMlStatus(),
    queryFn: () =>
      insightsJson<PriorityPage>("/insights/priority", {
        locale,
        query: { limit: 1 },
      }),
    enabled,
  });
}

export function useScoringRun(runId: string | null) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.insightsScoringRun(runId ?? ""),
    queryFn: () =>
      insightsJson<ScoringRun>(
        `/insights/scoring-runs/${encodeURIComponent(runId ?? "")}`,
        { locale },
      ),
    enabled: runId !== null,
    // Poll until the run is finished; the interval comes from the shared limits.
    refetchInterval: (query) =>
      scoringRunPollInterval(query.state.data?.status),
  });
}

// Starts a run. The run's progress is read by `useScoringRun`; when it finishes,
// the ranking and the scores it changed are refreshed.
export function useStartScoringRun() {
  const locale = useLocale();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (kind: "uplift" | "clv") =>
      insightsJson<ScoringRun>("/insights/scoring-runs", {
        method: "POST",
        body: { kind },
        locale,
      }),
    onSuccess: (run) => {
      queryClient.setQueryData(queryKeys.insightsScoringRun(run.id), run);
    },
  });
}

// Whether top-uplift targeting can run: the priority list reports `ready` only when
// ML is on, a model is active and a completed uplift run exists. The model shown
// next to the option is the one the scores come from.
export function useUpliftReadiness(enabled: boolean) {
  const status = useMlStatus(enabled);
  const models = useScoringModels(enabled);
  return {
    ready: status.data?.mlStatus === "ready",
    model: newestModelOf(models.data ?? [], "uplift"),
    isPending: status.isPending || models.isPending,
  };
}

// Re-reads the model registry from the service. Admins only; the server checks the
// permission. Returns how many models the sync found available.
export function useSyncScoringModels() {
  const locale = useLocale();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () =>
      insightsJson<{ synced: number | string }>("/insights/models/sync", {
        method: "POST",
        body: {},
        locale,
      }),
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: queryKeys.insightsModels() }),
  });
}
