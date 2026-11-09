"use client";

import { parseAsString, parseAsStringLiteral, useQueryState } from "nuqs";
import { useEffect, useState } from "react";

import { LIMITS } from "@/constants/limits";

const STATUS_VALUES = ["all", "open", "won", "lost"] as const;
export type DealStatusFilter = (typeof STATUS_VALUES)[number];

export function useDealFilters() {
  const [q, setQ] = useQueryState("q", parseAsString.withDefault(""));
  const [searchInput, setSearchInput] = useState(q);
  const [prevQ, setPrevQ] = useState(q);

  if (prevQ !== q) {
    setPrevQ(q);
    setSearchInput(q);
  }

  const [ownerUserId, setOwnerUserId] = useQueryState(
    "ownerUserId",
    parseAsString.withDefault(""),
  );

  const [companyId, setCompanyId] = useQueryState(
    "companyId",
    parseAsString.withDefault(""),
  );

  const [status, setStatus] = useQueryState(
    "status",
    parseAsStringLiteral(STATUS_VALUES).withDefault("open"),
  );

  useEffect(() => {
    const timer = setTimeout(() => {
      const trimmed = searchInput.trim();
      const nextQ =
        trimmed.length >= LIMITS.searchMinQueryLength ? trimmed : "";
      if (nextQ !== q) {
        void setQ(nextQ);
      }
    }, LIMITS.searchDebounceMs);

    return () => clearTimeout(timer);
  }, [searchInput, q, setQ]);

  const hasActiveFilters = Boolean(
    q || ownerUserId || companyId || (status && status !== "open"),
  );

  function clearFilters() {
    setSearchInput("");
    void setQ("");
    void setOwnerUserId("");
    void setCompanyId("");
    void setStatus("open");
  }

  return {
    q,
    searchInput,
    setSearchInput,
    ownerUserId,
    setOwnerUserId: (val: string) => void setOwnerUserId(val),
    companyId,
    setCompanyId: (val: string) => void setCompanyId(val),
    status,
    setStatus: (val: DealStatusFilter) => void setStatus(val),
    hasActiveFilters,
    clearFilters,
  };
}
