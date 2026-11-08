"use client";

import { useQuery } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { queryKeys } from "@/constants/query-keys";
import { backendJson } from "@/lib/api/bff-client";
import type { Company } from "@/features/crm/types";

export function useCompany(id: string, enabled = true) {
  const locale = useLocale();

  return useQuery({
    queryKey: queryKeys.company(id),
    enabled: Boolean(id) && enabled,
    queryFn: () =>
      backendJson<Company>(
        "/companies/{id}",
        { locale },
        `/companies/${encodeURIComponent(id)}`,
      ),
  });
}
