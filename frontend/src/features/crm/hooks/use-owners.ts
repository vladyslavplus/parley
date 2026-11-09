"use client";

import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import { backendJson } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";

export type OwnerOption = Schemas["OwnerView"];

export function useOwners(
  params?: { q?: string; limit?: number },
  options?: { enabled?: boolean },
) {
  const locale = useLocale();
  const q = params?.q?.trim() || undefined;
  const limit = Math.min(
    params?.limit ?? LIMITS.ownerPickerLimit,
    LIMITS.ownerPickerMaxLimit,
  );

  return useQuery({
    queryKey: queryKeys.ownersList({ q, limit }),
    enabled: options?.enabled ?? true,
    placeholderData: keepPreviousData,
    queryFn: async () => {
      const items = await backendJson<OwnerOption[]>("/owners", {
        locale,
        query: {
          q,
          limit,
        },
      });
      return { items };
    },
  });
}
