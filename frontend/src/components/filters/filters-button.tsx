"use client";

import * as Popover from "@radix-ui/react-popover";
import { ArrowLeft, Check, ChevronRight, ListFilter, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useId, useState, type KeyboardEvent } from "react";

import { TableProgressBar } from "@/components/data-table/table-progress-bar";
import type { FilterDimension, FilterOption } from "@/components/filters/types";
import { PICKER_TRIGGER } from "@/components/ui/input";
import { PickerSearch } from "@/components/ui/picker-search";
import { LIMITS } from "@/constants/limits";
import { cn } from "@/lib/utils";

interface FiltersButtonProps {
  dimensions: readonly FilterDimension[];
  className?: string;
}

function DimensionRow({
  dimension,
  onSelect,
}: {
  dimension: FilterDimension;
  onSelect: () => void;
}) {
  const valueLabel = dimension.useValueLabel(dimension.value);
  const Icon = dimension.icon;
  return (
    <button
      type="button"
      onClick={onSelect}
      className="flex w-full cursor-pointer items-center gap-2 rounded-[var(--radius-control)] px-2 py-2 text-left text-sm text-foreground transition-colors hover:bg-muted focus-visible:outline-2 focus-visible:outline-ring"
    >
      <Icon className="h-4 w-4 shrink-0 text-muted-foreground" />
      <span className="min-w-0 flex-1 truncate">{dimension.label}</span>
      {dimension.value ? (
        <span className="max-w-[45%] truncate rounded-xs bg-muted px-1.5 py-0.5 text-xs font-medium text-foreground">
          {valueLabel ?? "…"}
        </span>
      ) : null}
      <ChevronRight className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
    </button>
  );
}

function DimensionPanel({
  dimension,
  onBack,
}: {
  dimension: FilterDimension;
  onBack: () => void;
}) {
  const t = useTranslations("crm.filters");
  const listboxId = useId();
  const [searchInput, setSearchInput] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);

  useEffect(() => {
    const timer = setTimeout(
      () => setDebouncedQ(searchInput.trim()),
      LIMITS.searchDebounceMs,
    );
    return () => clearTimeout(timer);
  }, [searchInput]);

  const { options, isFetching } = dimension.useOptions(debouncedQ, true);
  const Icon = dimension.icon;

  function choose(option: FilterOption | null) {
    dimension.onChange(option?.id ?? "");
    onBack();
  }

  function handleKeyDown(event: KeyboardEvent) {
    if (event.key === "ArrowDown" && options.length > 0) {
      event.preventDefault();
      setActiveIndex((index) => (index + 1) % options.length);
    } else if (event.key === "ArrowUp" && options.length > 0) {
      event.preventDefault();
      setActiveIndex((index) => (index - 1 + options.length) % options.length);
    } else if (event.key === "Enter") {
      const option = options[activeIndex];
      if (option) {
        event.preventDefault();
        choose(option);
      }
    }
  }

  return (
    <div onKeyDown={handleKeyDown} className="relative">
      <TableProgressBar active={isFetching} />
      <div className="flex items-center gap-1 border-b border-border px-1 py-1">
        <button
          type="button"
          onClick={onBack}
          aria-label={t("back")}
          className="flex h-7 w-7 shrink-0 cursor-pointer items-center justify-center rounded-[var(--radius-control)] text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
        </button>
        <span className="flex min-w-0 items-center gap-1.5 text-sm font-medium text-foreground">
          <Icon className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
          <span className="truncate">{dimension.label}</span>
        </span>
        {dimension.value ? (
          <button
            type="button"
            onClick={() => choose(null)}
            className="ml-auto shrink-0 cursor-pointer rounded-[var(--radius-control)] px-2 py-1 text-xs font-medium text-muted-foreground transition-colors hover:bg-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
          >
            {t("clearFilter")}
          </button>
        ) : null}
      </div>

      <PickerSearch
        autoFocus
        role="combobox"
        aria-expanded
        aria-haspopup="listbox"
        aria-controls={listboxId}
        aria-activedescendant={
          options[activeIndex]
            ? `${listboxId}-${options[activeIndex].id}`
            : undefined
        }
        aria-autocomplete="list"
        aria-label={dimension.searchPlaceholder}
        placeholder={dimension.searchPlaceholder}
        value={searchInput}
        onValueChange={(value) => {
          setSearchInput(value);
          setActiveIndex(0);
        }}
        clearLabel={dimension.clearSearchLabel}
      />

      <div
        id={listboxId}
        role="listbox"
        aria-label={dimension.label}
        data-max-visible-rows={LIMITS.comboboxMaxVisibleRows}
        className="max-h-56 space-y-0.5 overflow-y-auto p-1"
      >
        {options.length === 0 && !isFetching ? (
          <div className="px-2 py-3 text-center text-xs text-muted-foreground">
            {dimension.emptyLabel}
          </div>
        ) : null}
        {options.map((option, index) => {
          const selected = option.id === dimension.value;
          return (
            <button
              key={option.id}
              id={`${listboxId}-${option.id}`}
              role="option"
              aria-selected={selected}
              type="button"
              onClick={() => choose(option)}
              className={cn(
                "flex w-full cursor-pointer items-center justify-between gap-2 rounded-[var(--radius-control)] px-2 py-1.5 text-left text-xs text-foreground transition-colors hover:bg-muted",
                index === activeIndex && "bg-muted",
              )}
            >
              <span className="flex min-w-0 flex-col">
                <span className="truncate font-medium">{option.label}</span>
                {option.description ? (
                  <span className="truncate font-mono text-[10px] text-muted-foreground">
                    {option.description}
                  </span>
                ) : null}
              </span>
              {selected ? (
                <Check className="h-3.5 w-3.5 shrink-0 text-primary" />
              ) : null}
            </button>
          );
        })}
      </div>
    </div>
  );
}

// Single entry point for every list-page filter: one button, a popover to pick
// the dimension, then a searchable list. The count badge shows what is active.
export function FiltersButton({ dimensions, className }: FiltersButtonProps) {
  const t = useTranslations("crm.filters");
  const [open, setOpen] = useState(false);
  const [activeKey, setActiveKey] = useState<string | null>(null);

  const activeCount = dimensions.filter((d) => d.value).length;
  const activeDimension = dimensions.find((d) => d.key === activeKey) ?? null;

  function handleOpenChange(next: boolean) {
    setOpen(next);
    if (!next) setActiveKey(null);
  }

  return (
    <Popover.Root open={open} onOpenChange={handleOpenChange}>
      <Popover.Trigger asChild>
        <button
          type="button"
          aria-haspopup="dialog"
          aria-label={
            activeCount > 0
              ? `${t("button")}, ${t("activeCount", { count: activeCount })}`
              : t("button")
          }
          className={cn(
            PICKER_TRIGGER,
            "w-auto justify-start gap-2 whitespace-nowrap font-medium",
            className,
          )}
        >
          <ListFilter className="h-3.5 w-3.5 shrink-0 text-muted-foreground" />
          <span>{t("button")}</span>
          {activeCount > 0 ? (
            <span className="min-w-5 rounded-full bg-primary px-1.5 text-center text-[11px] font-semibold leading-5 tabular-nums text-primary-foreground">
              {activeCount}
            </span>
          ) : null}
        </button>
      </Popover.Trigger>
      <Popover.Portal>
        <Popover.Content
          align="start"
          sideOffset={4}
          className="z-50 w-72 max-w-[calc(100vw-2rem)] overflow-hidden rounded-[var(--radius-card)] border border-border bg-popover p-0 text-popover-foreground shadow-[var(--shadow-floating)] outline-none"
        >
          {activeDimension ? (
            <DimensionPanel
              key={activeDimension.key}
              dimension={activeDimension}
              onBack={() => setActiveKey(null)}
            />
          ) : (
            <div className="p-1">
              {dimensions.map((dimension) => (
                <DimensionRow
                  key={dimension.key}
                  dimension={dimension}
                  onSelect={() => setActiveKey(dimension.key)}
                />
              ))}
            </div>
          )}
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}

function FilterChip({ dimension }: { dimension: FilterDimension }) {
  const t = useTranslations("crm.filters");
  const valueLabel = dimension.useValueLabel(dimension.value);
  const Icon = dimension.icon;
  const display = valueLabel ?? "…";
  return (
    <span className="inline-flex h-7 max-w-full items-center gap-1.5 rounded-full border border-border bg-muted pl-2.5 pr-1 text-xs text-foreground">
      <Icon className="h-3 w-3 shrink-0 text-muted-foreground" />
      <span className="shrink-0 text-muted-foreground">{dimension.label}:</span>
      <span className="min-w-0 max-w-48 truncate font-medium">{display}</span>
      <button
        type="button"
        onClick={() => dimension.onChange("")}
        aria-label={t("removeChip", {
          filter: dimension.label,
          value: display,
        })}
        className="flex h-5 w-5 shrink-0 cursor-pointer items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-background hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
      >
        <X className="h-3 w-3" />
      </button>
    </span>
  );
}

// Compact removable summary of the active dimensions; renders nothing if none.
export function FilterChips({
  dimensions,
  className,
}: {
  dimensions: readonly FilterDimension[];
  className?: string;
}) {
  const active = dimensions.filter((d) => d.value);
  if (active.length === 0) return null;
  return (
    <div className={cn("flex flex-wrap items-center gap-2", className)}>
      {active.map((dimension) => (
        <FilterChip key={dimension.key} dimension={dimension} />
      ))}
    </div>
  );
}
