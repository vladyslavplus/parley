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
import type { ContactFilters, ContactPage } from "@/features/crm/types";

export function contactListQueryOptions(
  filters: ContactFilters = {},
  locale: string,
) {
  const normalized = {
    q: filters.q || undefined,
    tagId: filters.tagId || undefined,
    companyId: filters.companyId || undefined,
    ownerUserId: filters.ownerUserId || undefined,
    segment: filters.segment || undefined,
    createdFrom: filters.createdFrom || undefined,
    createdTo: filters.createdTo || undefined,
    sort: filters.sort || "name",
    order: filters.order || "asc",
    after: filters.after || undefined,
    limit: filters.limit ?? LIMITS.defaultTablePageSize,
  };

  return queryOptions({
    queryKey: queryKeys.contactList(normalized),
    queryFn: () =>
      backendJson<ContactPage>(BACKEND_PATHS.contacts, {
        locale,
        query: normalized,
      }),
    placeholderData: keepPreviousData,
    staleTime: LIMITS.queryStaleTimeMs,
  });
}

export function useContacts(
  filters: ContactFilters = {},
  options?: { enabled?: boolean },
) {
  const locale = useLocale();
  return useQuery({
    ...contactListQueryOptions(filters, locale),
    ...(options?.enabled !== undefined ? { enabled: options.enabled } : {}),
  });
}
