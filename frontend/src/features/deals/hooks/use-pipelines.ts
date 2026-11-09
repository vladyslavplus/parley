"use client";

import {
  keepPreviousData,
  queryOptions,
  useQuery,
} from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import type {
  Pipeline,
  PipelineBoard,
  PipelineSummary,
} from "@/features/deals/types";
import { backendJson } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";

export function pipelinesListQueryOptions(locale: string) {
  return queryOptions({
    queryKey: queryKeys.pipelinesList(),
    queryFn: async () => {
      const page = await backendJson<
        Schemas["KeysetCursorPageOfPipelineSummaryView"]
      >("/pipelines", {
        locale,
        query: {
          sort: "name",
          order: "asc",
          limit: 10,
        },
      });
      return page.items;
    },
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

export function pipelineQueryOptions(id: string, locale: string) {
  return queryOptions({
    queryKey: queryKeys.pipeline(id),
    queryFn: () =>
      backendJson<Pipeline>(
        "/pipelines/{id}",
        {
          locale,
        },
        `/pipelines/${encodeURIComponent(id)}`,
      ),
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

export function pipelineBoardQueryOptions(id: string, locale: string) {
  return queryOptions({
    queryKey: queryKeys.pipelineBoard(id),
    queryFn: () =>
      backendJson<PipelineBoard>(
        "/pipelines/{id}/board",
        {
          locale,
        },
        `/pipelines/${encodeURIComponent(id)}/board`,
      ),
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

export function usePipelines() {
  const locale = useLocale();
  return useQuery(pipelinesListQueryOptions(locale));
}

export function usePipeline(id: string | null | undefined) {
  const locale = useLocale();
  return useQuery({
    ...pipelineQueryOptions(id ?? "", locale),
    enabled: Boolean(id),
  });
}

export function usePipelineBoard(id: string | null | undefined) {
  const locale = useLocale();
  return useQuery({
    ...pipelineBoardQueryOptions(id ?? "", locale),
    enabled: Boolean(id),
  });
}

export function useDefaultPipeline() {
  const pipelinesQuery = usePipelines();
  const defaultSummary: PipelineSummary | undefined =
    pipelinesQuery.data?.find((p) => p.isDefault) ?? pipelinesQuery.data?.[0];

  const pipelineId = defaultSummary?.id;
  const pipelineQuery = usePipeline(pipelineId);
  const boardQuery = usePipelineBoard(pipelineId);

  return {
    summary: defaultSummary,
    pipelines: pipelinesQuery.data ?? [],
    pipeline: pipelineQuery.data,
    board: boardQuery.data,
    isLoading:
      pipelinesQuery.isPending ||
      (Boolean(pipelineId) &&
        (pipelineQuery.isPending || boardQuery.isPending)),
    isFetching:
      pipelinesQuery.isFetching ||
      pipelineQuery.isFetching ||
      boardQuery.isFetching,
    refetch: async () => {
      await Promise.all([
        pipelinesQuery.refetch(),
        pipelineQuery.refetch(),
        boardQuery.refetch(),
      ]);
    },
  };
}
