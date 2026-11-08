"use client";

import { useQuery } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { queryKeys } from "@/constants/query-keys";
import { backendJson } from "@/lib/api/bff-client";
import type { CompanyFilters, CompanyPage } from "@/features/crm/types";

export function useCompanies(filters: CompanyFilters = {}) {
  const locale = useLocale();

  return useQuery({
    queryKey: queryKeys.companyList(filters as Record<string, unknown>),
    queryFn: () =>
      backendJson<CompanyPage>("/companies", {
        locale,
        query: {
          q: filters.q || undefined,
          ownerUserId: filters.ownerUserId || undefined,
          sort: filters.sort || undefined,
          order: filters.order || undefined,
          after: filters.after || undefined,
          limit: filters.limit ?? 25,
        },
      }),
  });
}
