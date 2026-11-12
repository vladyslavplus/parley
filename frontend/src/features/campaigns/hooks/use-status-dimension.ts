"use client";

import { Activity } from "lucide-react";
import { useTranslations } from "next-intl";

import type { FilterDimension } from "@/components/filters/types";
import { CAMPAIGN_STATUS_LIST } from "@/features/campaigns/types";

// The campaign status filter for the shared Filters button.
export function useStatusDimension(options: {
  value: string;
  onChange: (value: string) => void;
}): FilterDimension {
  const t = useTranslations("campaigns.status");
  const tList = useTranslations("campaigns.list");
  const tFilters = useTranslations("crm.filters");
  const label = (status: string) => t(status as "draft");

  return {
    key: "status",
    label: tList("filters.status"),
    icon: Activity,
    value: options.value,
    onChange: options.onChange,
    searchPlaceholder: tList("filters.statusSearch"),
    emptyLabel: tList("filters.statusEmpty"),
    clearSearchLabel: tFilters("clearSearch"),
    useOptions: (query) => ({
      options: CAMPAIGN_STATUS_LIST.filter((status) =>
        label(status).toLowerCase().includes(query.toLowerCase()),
      ).map((status) => ({ id: status, label: label(status) })),
      isFetching: false,
    }),
    useValueLabel: (value) =>
      CAMPAIGN_STATUS_LIST.some((status) => status === value)
        ? label(value)
        : null,
  };
}
