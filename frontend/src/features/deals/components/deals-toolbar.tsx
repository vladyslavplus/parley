"use client";

import { useTranslations } from "next-intl";

import { DataTableSearchFilterBar } from "@/components/data-table/data-table-search-filter-bar";
import {
  SegmentedControl,
  type SegmentedOption,
} from "@/components/ui/segmented-control";
import {
  useCompanyDimension,
  useOwnerDimension,
} from "@/features/crm/filters/use-filter-dimensions";
import type { DealStatusFilter } from "@/features/deals/hooks/use-deal-filters";

interface Props {
  searchInput: string;
  onSearchChange: (value: string) => void;
  ownerUserId: string;
  onOwnerChange: (ownerId: string | null) => void;
  companyId: string;
  onCompanyChange: (companyId: string | null) => void;
  status: DealStatusFilter;
  onStatusChange: (status: DealStatusFilter) => void;
  hasActiveFilters: boolean;
  onClearFilters: () => void;
}

export function DealsToolbar({
  searchInput,
  onSearchChange,
  ownerUserId,
  onOwnerChange,
  companyId,
  onCompanyChange,
  status,
  onStatusChange,
  hasActiveFilters,
  onClearFilters,
}: Props) {
  const t = useTranslations("crm.deals.toolbar");

  const ownerDimension = useOwnerDimension({
    value: ownerUserId,
    onChange: (value) => onOwnerChange(value || null),
  });
  const companyDimension = useCompanyDimension({
    value: companyId,
    onChange: (value) => onCompanyChange(value || null),
  });

  const statusOptions: SegmentedOption<DealStatusFilter>[] = [
    { value: "open", label: t("statusOpen") },
    { value: "won", label: t("statusWon") },
    { value: "lost", label: t("statusLost") },
    { value: "all", label: t("statusAll") },
  ];

  return (
    <DataTableSearchFilterBar
      searchInput={searchInput}
      onSearchInputChange={onSearchChange}
      onClearSearch={() => onSearchChange("")}
      searchPlaceholder={t("searchPlaceholder")}
      filters={[ownerDimension, companyDimension]}
      hasActiveFilters={hasActiveFilters}
      onResetFilters={onClearFilters}
    >
      <SegmentedControl
        value={status}
        options={statusOptions}
        onChange={onStatusChange}
        ariaLabel={t("statusLabel")}
      />
    </DataTableSearchFilterBar>
  );
}
