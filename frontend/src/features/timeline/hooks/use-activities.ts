"use client";

import { keepPreviousData, useInfiniteQuery } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import type { ActivityFilters, ActivityPage } from "@/features/timeline/types";
import { backendJson } from "@/lib/api/bff-client";

export function useActivities(filters: ActivityFilters) {
  const locale = useLocale();

  const hasSubject = Boolean(
    filters.contactId || filters.dealId || filters.companyId,
  );

  const filtersKey = {
    contactId: filters.contactId || undefined,
    dealId: filters.dealId || undefined,
    companyId: filters.companyId || undefined,
    type: filters.type || undefined,
    q: filters.q?.trim() || undefined,
    order: filters.order ?? "desc",
  };

  const infiniteQuery = useInfiniteQuery({
    queryKey: queryKeys.activitiesList(filtersKey),
    queryFn: async ({ pageParam }) => {
      return backendJson<ActivityPage>("/activities", {
        locale,
        query: {
          contactId: filters.contactId || undefined,
          dealId: filters.dealId || undefined,
          companyId: filters.companyId || undefined,
          type: filters.type || undefined,
          q: filters.q?.trim() || undefined,
          order: filters.order ?? "desc",
          limit: filters.limit ?? LIMITS.activitiesPageSize,
          after: pageParam,
        },
      });
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage) => lastPage.nextCursor ?? undefined,
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
    enabled: hasSubject,
  });

  const activities =
    infiniteQuery.data?.pages.flatMap((page) => page.items) ?? [];

  return {
    ...infiniteQuery,
    activities,
    hasMore: Boolean(infiniteQuery.hasNextPage),
    loadMore: infiniteQuery.fetchNextPage,
    isLoadingMore: infiniteQuery.isFetchingNextPage,
  };
}
