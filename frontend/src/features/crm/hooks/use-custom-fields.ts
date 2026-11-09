"use client";

import {
  keepPreviousData,
  queryOptions,
  useQuery,
} from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import { backendJson } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";

export type CustomFieldsPageParams = {
  after?: string;
  limit?: number;
  q?: string;
  entityType?: "contact" | "company";
  fieldType?: "text" | "number" | "date" | "boolean" | "select";
  sort?: "name" | "createdAt" | "position";
  order?: "asc" | "desc";
};

export function useCustomFields(
  entityType?: "contact" | "company",
  enabled = true,
) {
  const locale = useLocale();

  return useQuery({
    queryKey: queryKeys.customFields(entityType),
    enabled,
    queryFn: async () => {
      const page = await backendJson<
        Schemas["KeysetCursorPageOfCustomFieldDefinitionView"]
      >("/custom-fields", {
        locale,
        query: {
          entityType: entityType || undefined,
          sort: "position",
          order: "asc",
          limit: LIMITS.crmLookupLimit,
        },
      });
      return page.items;
    },
    placeholderData: keepPreviousData,
    staleTime: LIMITS.queryStaleTimeMs,
  });
}

export function customFieldsListQueryOptions(
  params: CustomFieldsPageParams = {},
  locale: string,
) {
  const normalized = {
    after: params.after || undefined,
    limit: params.limit ?? LIMITS.defaultTablePageSize,
    q: params.q || undefined,
    entityType: params.entityType || ("contact" as const),
    fieldType: params.fieldType || undefined,
    sort: params.sort || ("name" as const),
    order: params.order || ("asc" as const),
  };

  return queryOptions({
    queryKey: queryKeys.customFieldsList(normalized),
    queryFn: () =>
      backendJson<Schemas["KeysetCursorPageOfCustomFieldDefinitionView"]>(
        "/custom-fields",
        {
          locale,
          query: normalized,
        },
      ),
    placeholderData: keepPreviousData,
    staleTime: LIMITS.queryStaleTimeMs,
  });
}

export function useCustomFieldsPage(
  params: CustomFieldsPageParams = {},
  enabled = true,
) {
  const locale = useLocale();

  return useQuery({
    ...customFieldsListQueryOptions(params, locale),
    enabled,
  });
}
