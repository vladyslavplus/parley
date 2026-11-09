"use client";

import { LIMITS } from "@/constants/limits";
import { useMe } from "@/lib/api/hooks/use-api";

export function useOrganizationCurrency(): string {
  const { data: me } = useMe();
  return me?.organization?.currency || LIMITS.defaultCurrency;
}
