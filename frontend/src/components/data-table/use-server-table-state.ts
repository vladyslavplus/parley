"use client";

import type { OnChangeFn, SortingState } from "@tanstack/react-table";
import { parseAsString, parseAsStringLiteral, useQueryState } from "nuqs";
import { useEffect, useState } from "react";

import { LIMITS } from "@/constants/limits";
import { useKeysetPaging } from "./use-keyset-paging";

export interface ServerTableStateOptions<TSort extends string = string> {
  defaultSort: TSort;
  defaultOrder?: "asc" | "desc";
  allowedSorts?: readonly TSort[];
  cursorParamName?: string;
}

export function useServerTableState<TSort extends string = string>({
  defaultSort,
  defaultOrder = "asc",
  allowedSorts,
  cursorParamName = "after",
}: ServerTableStateOptions<TSort>) {
  const [q, setQ] = useQueryState("q", parseAsString.withDefault(""));
  const [searchInput, setSearchInput] = useState(q);
  const [prevQ, setPrevQ] = useState(q);
  if (prevQ !== q) {
    setPrevQ(q);
    setSearchInput(q);
  }

  const [sort, setSort] = useQueryState(
    "sort",
    parseAsString.withDefault(defaultSort),
  );
  const [order, setOrder] = useQueryState(
    "order",
    parseAsStringLiteral(["asc", "desc"] as const).withDefault(defaultOrder),
  );

  const paging = useKeysetPaging(cursorParamName);
  const { setCursor, resetPaging } = paging;

  useEffect(() => {
    const timer = setTimeout(() => {
      const trimmed = searchInput.trim();
      const nextQ =
        trimmed.length >= LIMITS.searchMinQueryLength ? trimmed : "";
      if (nextQ !== q) {
        void setQ(nextQ);
        void setCursor("");
      }
    }, LIMITS.searchDebounceMs);

    return () => clearTimeout(timer);
  }, [searchInput, q, setQ, setCursor]);

  const activeSortField: TSort =
    allowedSorts &&
    allowedSorts.length > 0 &&
    !(allowedSorts as readonly string[]).includes(sort)
      ? defaultSort
      : ((sort || defaultSort) as TSort);

  const sorting: SortingState = [
    { id: activeSortField, desc: order === "desc" },
  ];

  const onSortingChange: OnChangeFn<SortingState> = (updater) => {
    const next = typeof updater === "function" ? updater(sorting) : updater;
    if (next.length > 0) {
      const nextId = next[0].id;
      const validId =
        allowedSorts &&
        allowedSorts.length > 0 &&
        !(allowedSorts as readonly string[]).includes(nextId)
          ? defaultSort
          : nextId;
      void setSort(validId);
      void setOrder(next[0].desc ? "desc" : "asc");
    } else {
      void setSort(defaultSort);
      void setOrder(defaultOrder);
    }
    resetPaging();
  };

  function clearSearch() {
    setSearchInput("");
    void setQ("");
    resetPaging();
  }

  function resetSort(
    nextSort: string = defaultSort,
    nextOrder: "asc" | "desc" = defaultOrder,
  ) {
    void setSort(nextSort);
    void setOrder(nextOrder);
  }

  function resetAll(
    clearExtraFilters?: () => void,
    sortTarget?: { sort: string; order: "asc" | "desc" },
  ) {
    setSearchInput("");
    void setQ("");
    if (sortTarget) {
      void setSort(sortTarget.sort);
      void setOrder(sortTarget.order);
    }
    clearExtraFilters?.();
    resetPaging();
  }

  return {
    q,
    setQ,
    searchInput,
    setSearchInput,
    clearSearch,
    sort: activeSortField,
    setSort,
    order,
    setOrder,
    resetSort,
    sorting,
    onSortingChange,
    resetAll,
    ...paging,
  };
}
