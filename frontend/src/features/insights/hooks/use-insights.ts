"use client";

import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import { insightsJson } from "@/features/insights/lib/insights-api";
import {
  priorityQuery,
  type PriorityFilterValues,
  type PriorityPaging,
} from "@/features/insights/lib/priority-query";
import type {
  DashboardOverview,
  MlSettings,
  PriorityPage,
  RfmSegmentCount,
} from "@/features/insights/types";

// One keyset page of the priority list. The previous page stays on screen while
// the segment or the page changes.
export function usePriority(
  values: PriorityFilterValues,
  paging: PriorityPaging,
  enabled: boolean,
) {
  const locale = useLocale();
  const query = priorityQuery(values, paging);
  return useQuery({
    queryKey: queryKeys.insightsPriority(query),
    queryFn: () =>
      insightsJson<PriorityPage>("/insights/priority", { locale, query }),
    enabled,
    placeholderData: keepPreviousData,
  });
}

export function useRfmSegments(enabled: boolean) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.insightsRfmSegments(),
    queryFn: () =>
      insightsJson<RfmSegmentCount[]>("/insights/rfm/segments", { locale }),
    enabled,
    staleTime: LIMITS.queryStaleTimeMs,
  });
}

// Totals only. The server caches it for a short time; the client keeps it fresh
// for the same window.
export function useDashboardOverview(enabled: boolean) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.dashboardOverview(),
    queryFn: () =>
      insightsJson<DashboardOverview>("/dashboards/overview", { locale }),
    enabled,
    staleTime: LIMITS.queryStaleTimeMs,
  });
}

export function useMlSettings(enabled: boolean) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.insightsMlSettings(),
    queryFn: () =>
      insightsJson<MlSettings>("/insights/ml-settings", { locale }),
    enabled,
  });
}

// Saves the switch with the version the admin read. A 409 is handled by the
// caller; a successful save replaces the cached settings and the priority list.
export function useSaveMlSettings() {
  const locale = useLocale();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (input: { mlEnabled: boolean; version: number }) =>
      insightsJson<MlSettings>("/insights/ml-settings", {
        method: "PUT",
        body: input,
        locale,
      }),
    onSuccess: async (saved) => {
      queryClient.setQueryData(queryKeys.insightsMlSettings(), saved);
      await queryClient.invalidateQueries({
        queryKey: queryKeys.insightsMlSettings(),
      });
    },
  });
}
