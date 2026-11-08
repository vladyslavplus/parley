"use client";

import { useQuery } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { queryKeys } from "@/constants/query-keys";
import { backendJson } from "@/lib/api/bff-client";
import type { ContactFilters, ContactPage } from "@/features/crm/types";

export function useContacts(filters: ContactFilters = {}) {
  const locale = useLocale();

  return useQuery({
    queryKey: queryKeys.contactList(filters as Record<string, unknown>),
    queryFn: () =>
      backendJson<ContactPage>("/contacts", {
        locale,
        query: {
          q: filters.q || undefined,
          tagId: filters.tagId || undefined,
          companyId: filters.companyId || undefined,
          ownerUserId: filters.ownerUserId || undefined,
          createdFrom: filters.createdFrom || undefined,
          createdTo: filters.createdTo || undefined,
          sort: filters.sort || undefined,
          order: filters.order || undefined,
          after: filters.after || undefined,
          limit: filters.limit ?? 25,
        },
      }),
  });
}
