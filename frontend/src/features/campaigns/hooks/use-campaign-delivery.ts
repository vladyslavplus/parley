"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useLocale } from "next-intl";
import { useState } from "react";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import { HEADERS } from "@/constants/session";
import { shouldPollProgress } from "@/features/campaigns/progress/progress-state";
import type {
  Campaign,
  CampaignProgress,
  CampaignResults,
} from "@/features/campaigns/types";
import { backendJson, backendVoid } from "@/lib/api/bff-client";

function campaignPath(id: string, suffix: string): string {
  return `/campaigns/${encodeURIComponent(id)}/${suffix}`;
}

// Delivery counts. They are polled only while the campaign is sending; one
// more read happens when its status changes, so the final numbers show.
export function useCampaignProgress(id: string, status: string) {
  const locale = useLocale();
  return useQuery({
    queryKey: [...queryKeys.campaignProgress(id), status],
    queryFn: () =>
      backendJson<CampaignProgress>(
        "/campaigns/{id}/progress",
        { locale },
        campaignPath(id, "progress"),
      ),
    staleTime: 0,
    refetchInterval: shouldPollProgress(status)
      ? LIMITS.campaignProgressPollMs
      : false,
  });
}

export function useCampaignResults(id: string, enabled: boolean) {
  const locale = useLocale();
  // Set while a refresh is queued: the figure `computedAt` it must move past.
  const [waiting, setWaiting] = useState<{ before: string | null } | null>(
    null,
  );
  const queryClient = useQueryClient();

  const query = useQuery({
    queryKey: queryKeys.campaignResults(id),
    queryFn: () =>
      backendJson<CampaignResults>(
        "/campaigns/{id}/results",
        { locale },
        campaignPath(id, "results"),
      ),
    enabled,
    staleTime: 0,
    // After a refresh request the figures are polled until the job has
    // recomputed them (or the wait is given up in `refresh`).
    refetchInterval: (state) =>
      waiting !== null && state.state.data?.computedAt === waiting.before
        ? LIMITS.resultsRefreshPollMs
        : false,
  });

  async function refresh() {
    const before = query.data?.computedAt ?? null;
    await backendVoid(
      "/campaigns/{id}/results/refresh",
      { method: "POST", locale },
      campaignPath(id, "results/refresh"),
    );
    setWaiting({ before });
    setTimeout(() => setWaiting(null), LIMITS.resultsRefreshMaxMs);
    await queryClient.invalidateQueries({
      queryKey: queryKeys.campaignResults(id),
    });
  }

  const refreshing =
    waiting !== null && query.data?.computedAt === waiting.before;

  return { ...query, refresh, refreshing };
}

// Actions of the send step. Starting takes the Idempotency-Key of the current
// confirmation so a retry cannot start the campaign twice.
export function useCampaignSending(id: string) {
  const locale = useLocale();
  const queryClient = useQueryClient();

  async function start(idempotencyKey: string): Promise<Campaign> {
    const started = await backendJson<Campaign>(
      "/campaigns/{id}/start",
      {
        method: "POST",
        locale,
        headers: { [HEADERS.idempotencyKey]: idempotencyKey },
      },
      campaignPath(id, "start"),
    );
    queryClient.setQueryData(queryKeys.campaign(id), started);
    await queryClient.invalidateQueries({
      queryKey: queryKeys.campaignProgress(id),
    });
    return started;
  }

  function sendTest(): Promise<void> {
    return backendVoid(
      "/campaigns/{id}/test-send",
      { method: "POST", locale },
      campaignPath(id, "test-send"),
    );
  }

  return { start, sendTest };
}
