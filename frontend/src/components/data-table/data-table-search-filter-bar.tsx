"use client";

import { Search, X } from "lucide-react";
import { useTranslations } from "next-intl";
import type { ReactNode } from "react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";

export interface DataTableSearchFilterBarProps {
  searchInput: string;
  onSearchInputChange: (value: string) => void;
  onClearSearch: () => void;
  searchPlaceholder: string;
  hasActiveFilters?: boolean;
  onResetFilters?: () => void;
  children?: ReactNode;
  actions?: ReactNode;
}

export function DataTableSearchFilterBar({
  searchInput,
  onSearchInputChange,
  onClearSearch,
  searchPlaceholder,
  hasActiveFilters = false,
  onResetFilters,
  children,
  actions,
}: DataTableSearchFilterBarProps) {
  const tFilters = useTranslations("crm.filters");

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 w-full">
      <div className="flex flex-wrap items-center gap-2 flex-1 min-w-[260px]">
        <div className="relative w-full max-w-xs">
          <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
          <Input
            value={searchInput}
            onChange={(e) => onSearchInputChange(e.target.value)}
            placeholder={searchPlaceholder}
            aria-label={searchPlaceholder}
            className="h-8 pl-8 pr-7 text-xs bg-background"
          />
          {searchInput ? (
            <button
              type="button"
              onClick={onClearSearch}
              className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground cursor-pointer"
              aria-label={tFilters("clearSearch")}
            >
              <X className="h-3.5 w-3.5" />
            </button>
          ) : null}
        </div>

        {children}

        {hasActiveFilters && onResetFilters ? (
          <Button
            variant="secondary"
            onClick={onResetFilters}
            className="h-8 px-2.5 text-xs text-muted-foreground whitespace-nowrap"
          >
            {tFilters("reset")}
          </Button>
        ) : null}
      </div>

      {actions ? (
        <div className="flex items-center gap-2">{actions}</div>
      ) : null}
    </div>
  );
}
