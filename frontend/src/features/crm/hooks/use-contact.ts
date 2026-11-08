"use client";

import { useQuery } from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { queryKeys } from "@/constants/query-keys";
import { backendJson } from "@/lib/api/bff-client";
import type { Contact } from "@/features/crm/types";

export function useContact(id: string, enabled = true) {
  const locale = useLocale();

  return useQuery({
    queryKey: queryKeys.contact(id),
    enabled: Boolean(id) && enabled,
    queryFn: () =>
      backendJson<Contact>(
        "/contacts/{id}",
        { locale },
        `/contacts/${encodeURIComponent(id)}`,
      ),
  });
}
