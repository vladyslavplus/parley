"use client";

import { RotateCcw, Search, X } from "lucide-react";
import { useTranslations } from "next-intl";
import type { ReactNode } from "react";

import {
  FilterChips,
  FiltersButton,
} from "@/components/filters/filters-button";
import type { FilterDimension } from "@/components/filters/types";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export interface DataTableSearchFilterBarProps {
  searchInput: string;
  onSearchInputChange: (value: string) => void;
  onClearSearch: () => void;
  searchPlaceholder: string;
  hasActiveFilters?: boolean;
  onResetFilters?: () => void;
  // Dimensions rendered behind the shared "Filters" button plus chips.
  filters?: readonly FilterDimension[];
  children?: ReactNode;
  actions?: ReactNode;
}

// The one toolbar for every list page: search, Filters button, optional
// segmented control (children), reset, and page actions share one height.
export function DataTableSearchFilterBar({
  searchInput,
  onSearchInputChange,
  onClearSearch,
  searchPlaceholder,
  hasActiveFilters = false,
  onResetFilters,
  filters,
  children,
  actions,
}: DataTableSearchFilterBarProps) {
  const tFilters = useTranslations("crm.filters");
  const hasFilterChips = Boolean(filters?.some((filter) => filter.value));

  return (
    <div className="flex w-full flex-col gap-2">
      <div className="flex w-full flex-wrap items-center justify-between gap-3">
        <div className="flex min-w-[260px] flex-1 flex-wrap items-center gap-2">
          <div className="relative w-full max-w-xs">
            <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
            <Input
              value={searchInput}
              onChange={(e) => onSearchInputChange(e.target.value)}
              placeholder={searchPlaceholder}
              aria-label={searchPlaceholder}
              className="bg-background pl-8 pr-9 text-xs"
            />
            {searchInput ? (
              <button
                type="button"
                onClick={onClearSearch}
                className="absolute right-2 top-1/2 flex h-6 w-6 -translate-y-1/2 cursor-pointer items-center justify-center rounded-[var(--radius-control)] text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
                aria-label={tFilters("clearSearch")}
              >
                <X className="h-3.5 w-3.5" />
              </button>
            ) : null}
          </div>

          {filters && filters.length > 0 ? (
            <FiltersButton dimensions={filters} />
          ) : null}

          {children}

          {hasActiveFilters && onResetFilters ? (
            <Button
              type="button"
              variant="secondary"
              onClick={onResetFilters}
              className="gap-1.5 px-2.5 text-xs text-muted-foreground hover:text-foreground"
            >
              <RotateCcw className="h-3.5 w-3.5" />
              <span>{tFilters("reset")}</span>
            </Button>
          ) : null}
        </div>

        {actions ? (
          <div className="flex items-center gap-2">{actions}</div>
        ) : null}
      </div>

      {hasFilterChips && filters ? <FilterChips dimensions={filters} /> : null}
    </div>
  );
}
