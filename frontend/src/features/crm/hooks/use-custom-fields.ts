"use client";

import { useQuery } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { queryKeys } from "@/constants/query-keys";
import { backendJson } from "@/lib/api/bff-client";
import type { CustomFieldDefinition } from "@/features/crm/types";

export function useCustomFields(
  entityType?: "contact" | "company" | "deal",
  enabled = true,
) {
  const locale = useLocale();

  return useQuery({
    queryKey: queryKeys.customFields(entityType),
    enabled,
    queryFn: () =>
      backendJson<CustomFieldDefinition[]>("/custom-fields", {
        locale,
        query: entityType ? { entityType } : undefined,
      }),
  });
}
