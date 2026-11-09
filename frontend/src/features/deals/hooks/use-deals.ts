"use client";

import {
  keepPreviousData,
  queryOptions,
  useInfiniteQuery,
  useQuery,
} from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import type { Deal, DealFilters, DealPage } from "@/features/deals/types";
import { backendJson } from "@/lib/api/bff-client";

export function dealQueryOptions(id: string, locale: string) {
  return queryOptions({
    queryKey: queryKeys.deal(id),
    queryFn: () =>
      backendJson<Deal>(
        "/deals/{id}",
        {
          locale,
        },
        `/deals/${encodeURIComponent(id)}`,
      ),
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

export function dealListQueryOptions(filters: DealFilters, locale: string) {
  return queryOptions({
    queryKey: queryKeys.dealList(filters as Record<string, unknown>),
    queryFn: () =>
      backendJson<DealPage>("/deals", {
        locale,
        query: {
          q: filters.q || undefined,
          stageId: filters.stageId || undefined,
          ownerUserId: filters.ownerUserId || undefined,
          companyId: filters.companyId || undefined,
          contactId: filters.contactId || undefined,
          status: filters.status || undefined,
          sort: filters.sort || "position",
          order: filters.order || "asc",
          limit: filters.limit ?? LIMITS.kanbanColumnPageSize,
          after: filters.after || undefined,
        },
      }),
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

export function useDeal(id: string | null | undefined) {
  const locale = useLocale();
  return useQuery({
    ...dealQueryOptions(id ?? "", locale),
    enabled: Boolean(id),
  });
}

export function useDeals(
  filters: DealFilters,
  options?: { enabled?: boolean },
) {
  const locale = useLocale();
  return useQuery({
    ...dealListQueryOptions(filters, locale),
    ...options,
  });
}

export function normalizeStageDealsFilters(
  extraFilters?: Omit<DealFilters, "stageId" | "sort">,
) {
  return {
    q: extraFilters?.q?.trim() || undefined,
    ownerUserId: extraFilters?.ownerUserId || undefined,
    companyId: extraFilters?.companyId || undefined,
    contactId: extraFilters?.contactId || undefined,
    status: extraFilters?.status || undefined,
  };
}

export function stageDealsQueryKey(
  stageId: string,
  extraFilters?: Omit<DealFilters, "stageId" | "sort">,
) {
  return queryKeys.dealList({
    stageId,
    sort: "position",
    order: "asc",
    ...normalizeStageDealsFilters(extraFilters),
  });
}

export function stageDealsQueryOptions(
  stageId: string,
  extraFilters: Omit<DealFilters, "stageId" | "sort"> | undefined,
  locale: string,
) {
  const norm = normalizeStageDealsFilters(extraFilters);
  return {
    queryKey: stageDealsQueryKey(stageId, extraFilters),
    queryFn: async ({ pageParam }: { pageParam?: string }) => {
      return backendJson<DealPage>("/deals", {
        locale,
        query: {
          stageId,
          sort: "position",
          order: "asc",
          limit: LIMITS.kanbanColumnPageSize,
          after: pageParam,
          ...norm,
        },
      });
    },
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage: DealPage) => lastPage.nextCursor ?? undefined,
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  };
}

export function useStageDeals(
  stageId: string,
  extraFilters?: Omit<DealFilters, "stageId" | "sort">,
) {
  const locale = useLocale();
  const infiniteQuery = useInfiniteQuery(
    stageDealsQueryOptions(stageId, extraFilters, locale),
  );

  const deals = infiniteQuery.data?.pages.flatMap((page) => page.items) ?? [];

  return {
    ...infiniteQuery,
    deals,
    hasMore: Boolean(infiniteQuery.hasNextPage),
    loadMore: infiniteQuery.fetchNextPage,
    isLoadingMore: infiniteQuery.isFetchingNextPage,
  };
}
