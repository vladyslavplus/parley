"use client";

import {
  keepPreviousData,
  queryOptions,
  useQuery,
} from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import { BACKEND_PATHS } from "@/constants/routes";
import { backendJson } from "@/lib/api/bff-client";
import type { CompanyFilters, CompanyPage } from "@/features/crm/types";

export function companyListQueryOptions(
  filters: CompanyFilters = {},
  locale: string,
) {
  const normalized = {
    q: filters.q || undefined,
    ownerUserId: filters.ownerUserId || undefined,
    sort: filters.sort || "name",
    order: filters.order || "asc",
    after: filters.after || undefined,
    limit: filters.limit ?? LIMITS.defaultTablePageSize,
  };

  return queryOptions({
    queryKey: queryKeys.companyList(normalized),
    queryFn: () =>
      backendJson<CompanyPage>(BACKEND_PATHS.companies, {
        locale,
        query: normalized,
      }),
    placeholderData: keepPreviousData,
    staleTime: LIMITS.queryStaleTimeMs,
  });
}

export function useCompanies(
  filters: CompanyFilters = {},
  options?: { enabled?: boolean },
) {
  const locale = useLocale();
  return useQuery({
    ...companyListQueryOptions(filters, locale),
    ...(options?.enabled !== undefined ? { enabled: options.enabled } : {}),
  });
}
