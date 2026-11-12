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
  Segment,
  SegmentDefinition,
  SegmentFilters,
  SegmentPage,
  SegmentPreview,
  Template,
  TemplateFilters,
  TemplatePage,
  TemplatePreview,
} from "@/features/campaigns/types";
import { backendJson } from "@/lib/api/bff-client";

export function useTemplates(
  filters: TemplateFilters = {},
  options?: { enabled?: boolean },
) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.templatesList({ ...filters }),
    queryFn: () =>
      backendJson<TemplatePage>("/templates", {
        locale,
        query: {
          q: filters.q?.trim() || undefined,
          after: filters.after ?? undefined,
          limit: filters.limit ?? LIMITS.libraryPageSize,
        },
      }),
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
    enabled: options?.enabled ?? true,
  });
}

export function useSegments(
  filters: SegmentFilters = {},
  options?: { enabled?: boolean },
) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.segmentsList({ ...filters }),
    queryFn: () =>
      backendJson<SegmentPage>("/segments", {
        locale,
        query: {
          q: filters.q?.trim() || undefined,
          after: filters.after ?? undefined,
          limit: filters.limit ?? LIMITS.libraryPageSize,
        },
      }),
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
    enabled: options?.enabled ?? true,
  });
}

// Full template (source included): needed by the editor, not by the list.
export function useTemplate(id: string | null | undefined) {
  const locale = useLocale();
  return useQuery({
    queryKey: [...queryKeys.templatesAll(), "detail", id] as const,
    queryFn: () =>
      backendJson<Template>(
        "/templates/{id}",
        { locale },
        `/templates/${encodeURIComponent(id ?? "")}`,
      ),
    enabled: Boolean(id),
  });
}

export function useSegment(id: string | null | undefined) {
  const locale = useLocale();
  return useQuery({
    queryKey: [...queryKeys.segmentsAll(), "detail", id] as const,
    queryFn: () =>
      backendJson<Segment>(
        "/segments/{id}",
        { locale },
        `/segments/${encodeURIComponent(id ?? "")}`,
      ),
    enabled: Boolean(id),
  });
}

// Server-rendered sample of the SAVED template. The key carries the version so
// a save refreshes the preview and nothing else does.
export function templatePreviewQueryOptions(
  id: string,
  version: number | string,
  locale: string,
) {
  return queryOptions({
    queryKey: queryKeys.templatePreview(id, version),
    queryFn: () =>
      backendJson<TemplatePreview>(
        "/templates/{id}/preview",
        { method: "POST", locale },
        `/templates/${encodeURIComponent(id)}/preview`,
      ),
    retry: false,
  });
}

export function useTemplatePreview(
  id: string | null | undefined,
  version: number | string | undefined,
) {
  const locale = useLocale();
  return useQuery({
    ...templatePreviewQueryOptions(id ?? "", version ?? 0, locale),
    enabled: Boolean(id) && version !== undefined,
  });
}

export function useSegmentPreview(
  definition: SegmentDefinition,
  enabled: boolean,
) {
  const locale = useLocale();
  return useQuery({
    queryKey: queryKeys.segmentPreview(definition),
    queryFn: () =>
      backendJson<SegmentPreview>("/segments/preview", {
        method: "POST",
        locale,
        body: definition,
      }),
    enabled,
    retry: false,
    placeholderData: keepPreviousData,
  });
}
