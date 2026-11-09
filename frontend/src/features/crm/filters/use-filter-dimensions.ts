"use client";

import { Building2, Tag as TagIcon, User } from "lucide-react";
import { useTranslations } from "next-intl";

import type {
  FilterDimension,
  FilterOption,
  FilterOptionsResult,
} from "@/components/filters/types";
import { LIMITS } from "@/constants/limits";
import { useCompanies } from "@/features/crm/hooks/use-companies";
import { useCompany } from "@/features/crm/hooks/use-company";
import { useOwners } from "@/features/crm/hooks/use-owners";
import { useTags } from "@/features/crm/hooks/use-tags";

interface DimensionInput {
  value: string;
  onChange: (value: string) => void;
  label?: string;
}

function useOwnerOptions(query: string, enabled: boolean): FilterOptionsResult {
  const result = useOwners(
    { q: query || undefined, limit: LIMITS.ownerPickerLimit },
    { enabled },
  );
  const options: FilterOption[] = (result.data?.items ?? []).map((owner) => ({
    id: owner.id,
    label: owner.displayName || owner.email || owner.id,
    description: owner.displayName && owner.email ? owner.email : null,
  }));
  return { options, isFetching: result.isFetching };
}

function useOwnerLabel(value: string): string | null {
  const result = useOwners(
    { limit: LIMITS.ownerPickerMaxLimit },
    { enabled: Boolean(value) },
  );
  if (!value) return null;
  const owner = result.data?.items.find((item) => item.id === value);
  return owner ? owner.displayName || owner.email || owner.id : null;
}

function useCompanyOptions(
  query: string,
  enabled: boolean,
): FilterOptionsResult {
  const result = useCompanies(
    { q: query || undefined, limit: LIMITS.companyPickerLimit },
    { enabled },
  );
  const options: FilterOption[] = (result.data?.items ?? []).map((company) => ({
    id: company.id,
    label: company.name,
    description: company.domain ?? null,
  }));
  return { options, isFetching: result.isFetching };
}

function useCompanyLabel(value: string): string | null {
  const result = useCompany(value, Boolean(value));
  return value ? (result.data?.name ?? null) : null;
}

function useTagOptions(query: string, enabled: boolean): FilterOptionsResult {
  const result = useTags({
    q: query || undefined,
    limit: LIMITS.tagPickerLimit,
    enabled,
  });
  const options: FilterOption[] = (result.data?.items ?? []).map((tag) => ({
    id: tag.id,
    label: tag.name,
  }));
  return { options, isFetching: result.isFetching };
}

function useTagLabel(value: string): string | null {
  const result = useTags({
    limit: LIMITS.crmLookupLimit,
    enabled: Boolean(value),
  });
  if (!value) return null;
  return result.data?.items.find((tag) => tag.id === value)?.name ?? null;
}

export function useOwnerDimension({
  value,
  onChange,
  label,
}: DimensionInput): FilterDimension {
  const t = useTranslations("crm.filters");
  return {
    key: "owner",
    label: label ?? t("owner"),
    icon: User,
    value,
    onChange,
    searchPlaceholder: t("ownerPicker.searchPlaceholder"),
    emptyLabel: t("ownerPicker.empty"),
    clearSearchLabel: t("clearSearch"),
    useOptions: useOwnerOptions,
    useValueLabel: useOwnerLabel,
  };
}

export function useCompanyDimension({
  value,
  onChange,
  label,
}: DimensionInput): FilterDimension {
  const t = useTranslations("crm.filters");
  const tCompanies = useTranslations("crm.companies");
  return {
    key: "company",
    label: label ?? t("company"),
    icon: Building2,
    value,
    onChange,
    searchPlaceholder: tCompanies("picker.searchPlaceholder"),
    emptyLabel: tCompanies("picker.empty"),
    clearSearchLabel: t("clearSearch"),
    useOptions: useCompanyOptions,
    useValueLabel: useCompanyLabel,
  };
}

export function useTagDimension({
  value,
  onChange,
  label,
}: DimensionInput): FilterDimension {
  const t = useTranslations("crm.filters");
  return {
    key: "tag",
    label: label ?? t("tag"),
    icon: TagIcon,
    value,
    onChange,
    searchPlaceholder: t("tagPicker.searchPlaceholder"),
    emptyLabel: t("tagPicker.emptySearch"),
    clearSearchLabel: t("clearSearch"),
    useOptions: useTagOptions,
    useValueLabel: useTagLabel,
  };
}
