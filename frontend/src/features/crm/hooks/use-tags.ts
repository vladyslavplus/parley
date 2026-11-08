"use client";

import { useQuery } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { queryKeys } from "@/constants/query-keys";
import { backendJson } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";

type TagPage = Schemas["KeysetCursorPageOfTagView"];

export function useTags(enabled = true) {
  const locale = useLocale();

  return useQuery({
    queryKey: queryKeys.tags(),
    enabled,
    queryFn: () =>
      backendJson<TagPage>("/tags", {
        locale,
        query: { limit: 100 },
      }),
  });
}
