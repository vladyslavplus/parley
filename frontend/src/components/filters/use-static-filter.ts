"use client";

import type { LucideIcon } from "lucide-react";
import { useTranslations } from "next-intl";

import type { FilterDimension, FilterOption } from "@/components/filters/types";

interface StaticFilterConfig {
  key: string;
  label: string;
  icon: LucideIcon;
  value: string;
  onChange: (value: string) => void;
  options: readonly FilterOption[];
  searchPlaceholder: string;
  emptyLabel: string;
}

// A filter with a fixed option list for the shared Filters button. Its options
// are plain data, so it works like the other dimensions without a server read.
export function useStaticFilter(config: StaticFilterConfig): FilterDimension {
  const tFilters = useTranslations("crm.filters");
  const { options, value } = config;
  return {
    key: config.key,
    label: config.label,
    icon: config.icon,
    value,
    onChange: config.onChange,
    searchPlaceholder: config.searchPlaceholder,
    emptyLabel: config.emptyLabel,
    clearSearchLabel: tFilters("clearSearch"),
    useOptions: (query) => ({
      options: options.filter((option) =>
        option.label.toLowerCase().includes(query.trim().toLowerCase()),
      ),
      isFetching: false,
    }),
    useValueLabel: (current) =>
      options.find((option) => option.id === current)?.label ?? null,
  };
}
