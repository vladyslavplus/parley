"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import { backendJson } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";

type TagPage = Schemas["KeysetCursorPageOfTagView"];

export function useTags(
  paramsOrEnabled?: boolean | { q?: string; limit?: number; enabled?: boolean },
) {
  const locale = useLocale();
  const isBoolean = typeof paramsOrEnabled === "boolean";
  const enabled = isBoolean
    ? paramsOrEnabled
    : (paramsOrEnabled?.enabled ?? true);
  const q = !isBoolean ? paramsOrEnabled?.q?.trim() || undefined : undefined;
  const limit = !isBoolean
    ? (paramsOrEnabled?.limit ?? LIMITS.crmLookupLimit)
    : LIMITS.crmLookupLimit;

  return useQuery({
    queryKey:
      q || limit !== LIMITS.crmLookupLimit
        ? queryKeys.tagsList({ q, limit })
        : queryKeys.tags(),
    enabled,
    placeholderData: keepPreviousData,
    queryFn: () =>
      backendJson<TagPage>("/tags", {
        locale,
        query: { q, limit },
      }),
  });
}
