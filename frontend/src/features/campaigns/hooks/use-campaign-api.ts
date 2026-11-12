"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import {
  CAMPAIGN_STATUSES,
  type Campaign,
  type CampaignFilters,
  type CampaignPage,
  type CampaignReview,
} from "@/features/campaigns/types";
import { shouldPollProgress } from "@/features/campaigns/progress/progress-state";
import { backendJson } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";

export function useCampaignApi() {
  const locale = useLocale();

  function createCampaign(body: Schemas["CampaignRequest"]): Promise<Campaign> {
    return backendJson<Campaign>("/campaigns", {
      method: "POST",
      body,
      locale,
    });
  }

  function updateCampaign(
    id: string,
    body: Schemas["CampaignRequest"],
  ): Promise<Campaign> {
    return backendJson<Campaign>(
      "/campaigns/{id}",
      { method: "PUT", body, locale },
      `/campaigns/${encodeURIComponent(id)}`,
    );
  }

  function prepareCampaign(id: string): Promise<Campaign> {
    return backendJson<Campaign>(
      "/campaigns/{id}/prepare",
      { method: "POST", locale },
      `/campaigns/${encodeURIComponent(id)}/prepare`,
    );
  }

  function getCampaign(id: string): Promise<Campaign> {
    return backendJson<Campaign>(
      "/campaigns/{id}",
      { locale },
      `/campaigns/${encodeURIComponent(id)}`,
    );
  }

  return { createCampaign, updateCampaign, prepareCampaign, getCampaign };
}

// Review counts. While the audience is being frozen the query polls until the
// campaign leaves the `preparing` status.
export function useCampaignReview(
  id: string | null,
  enabled: boolean,
  poll = true,
) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.campaignReview(id ?? ""),
    queryFn: () =>
      backendJson<CampaignReview>(
        "/campaigns/{id}/review",
        { locale },
        `/campaigns/${encodeURIComponent(id ?? "")}/review`,
      ),
    enabled: Boolean(id) && enabled,
    staleTime: 0,
    refetchInterval: (query) =>
      poll && query.state.data?.status === CAMPAIGN_STATUSES.preparing
        ? LIMITS.campaignReviewPollMs
        : false,
  });
}

export function useCampaigns(filters: CampaignFilters = {}) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.campaignsList({ ...filters }),
    queryFn: () =>
      backendJson<CampaignPage>("/campaigns", {
        locale,
        query: {
          q: filters.q?.trim() || undefined,
          status: filters.status || undefined,
          sort: filters.sort || undefined,
          order: filters.order || undefined,
          after: filters.after ?? undefined,
          limit: filters.limit ?? LIMITS.libraryPageSize,
        },
      }),
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

// One campaign by id; the server version is what the builder resumes from.
export function useCampaign(id: string) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.campaign(id),
    queryFn: () =>
      backendJson<Campaign>(
        "/campaigns/{id}",
        { locale },
        `/campaigns/${encodeURIComponent(id)}`,
      ),
    staleTime: 0,
    retry: false,
    // A sending campaign changes status on its own (to completed).
    refetchInterval: (state) =>
      shouldPollProgress(state.state.data?.status)
        ? LIMITS.campaignProgressPollMs
        : false,
  });
}
