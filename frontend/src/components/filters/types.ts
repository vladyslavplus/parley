import type { LucideIcon } from "lucide-react";

export interface FilterOption {
  id: string;
  label: string;
  description?: string | null;
}

export interface FilterOptionsResult {
  options: FilterOption[];
  isFetching: boolean;
}

// One filterable dimension (owner, company, tag...). The hooks are called from
// inside the panel/chip components, so a dimension stays a plain object.
export interface FilterDimension {
  key: string;
  label: string;
  icon: LucideIcon;
  value: string;
  onChange: (value: string) => void;
  searchPlaceholder: string;
  emptyLabel: string;
  clearSearchLabel: string;
  useOptions: (query: string, enabled: boolean) => FilterOptionsResult;
  useValueLabel: (value: string) => string | null;
}
