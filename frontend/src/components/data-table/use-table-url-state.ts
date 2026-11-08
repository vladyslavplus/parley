"use client";

import type {
  OnChangeFn,
  PaginationState,
  SortingState,
} from "@tanstack/react-table";
import {
  createParser,
  parseAsString,
  parseAsStringLiteral,
  useQueryState,
} from "nuqs";

import { LIMITS } from "@/constants/limits";

const pageParser = createParser({
  parse: (query) => {
    const n = parseInt(query, 10);
    return isNaN(n) ? 1 : Math.max(1, n);
  },
  serialize: (value) => String(value),
})
  .withDefault(1)
  .withOptions({ history: "replace" });

function createPageSizeParser(defaultPageSize: number) {
  return createParser({
    parse: (query) => {
      const n = parseInt(query, 10);
      const validSizes: readonly number[] = LIMITS.tablePageSizes;
      if (isNaN(n) || !validSizes.includes(n)) {
        return defaultPageSize;
      }
      return n;
    },
    serialize: (value) => String(value),
  })
    .withDefault(defaultPageSize)
    .withOptions({ history: "replace" });
}

const orderParser = parseAsStringLiteral(["asc", "desc"] as const)
  .withDefault("asc")
  .withOptions({ history: "replace" });

const searchParser = parseAsString
  .withDefault("")
  .withOptions({ history: "replace" });

const sortParser = parseAsString
  .withDefault("")
  .withOptions({ history: "replace" });

export function useTableUrlState({
  defaultPageSize = LIMITS.defaultTablePageSize,
}: { defaultPageSize?: number } = {}) {
  const [search, setSearch] = useQueryState("q", searchParser);
  const [sortBy, setSortBy] = useQueryState("sort", sortParser);
  const [sortOrder, setSortOrder] = useQueryState("order", orderParser);
  const [page, setPage] = useQueryState("page", pageParser);
  const [pageSize, setPageSize] = useQueryState(
    "size",
    createPageSizeParser(defaultPageSize),
  );

  const sorting: SortingState = sortBy
    ? [{ id: sortBy, desc: sortOrder === "desc" }]
    : [];

  const pagination: PaginationState = {
    pageIndex: Math.max(0, page - 1),
    pageSize,
  };

  const onSortingChange: OnChangeFn<SortingState> = (updaterOrValue) => {
    const nextSorting =
      typeof updaterOrValue === "function"
        ? updaterOrValue(sorting)
        : updaterOrValue;

    if (nextSorting.length > 0) {
      setSortBy(nextSorting[0].id);
      setSortOrder(nextSorting[0].desc ? "desc" : "asc");
    } else {
      setSortBy("");
      setSortOrder("asc");
    }
    setPage(1);
  };

  const onPaginationChange: OnChangeFn<PaginationState> = (updaterOrValue) => {
    const nextPagination =
      typeof updaterOrValue === "function"
        ? updaterOrValue(pagination)
        : updaterOrValue;

    setPage(nextPagination.pageIndex + 1);
    setPageSize(nextPagination.pageSize);
  };

  const onGlobalFilterChange: OnChangeFn<string> = (updaterOrValue) => {
    const nextFilter =
      typeof updaterOrValue === "function"
        ? updaterOrValue(search)
        : updaterOrValue;

    setSearch(nextFilter);
    setPage(1);
  };

  return {
    search,
    setSearch,
    sortBy,
    setSortBy,
    sortOrder,
    setSortOrder,
    page,
    setPage,
    pageSize,
    setPageSize,
    sorting,
    pagination,
    onSortingChange,
    onPaginationChange,
    onGlobalFilterChange,
  };
}
