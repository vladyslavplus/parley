"use client";

import * as Popover from "@radix-ui/react-popover";
import { Check, ChevronsUpDown, Search, User, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";

import { TableProgressBar } from "@/components/data-table/table-progress-bar";
import { Input } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import { useOwners, type OwnerOption } from "@/features/crm/hooks/use-owners";
import { cn } from "@/lib/utils";

export interface OwnerPickerProps {
  value?: string | null;
  valueDisplayName?: string | null;
  valueEmail?: string | null;
  onChange: (ownerId: string | null, owner?: OwnerOption | null) => void;
  allowClear?: boolean;
  clearLabel?: string;
  disabled?: boolean;
  placeholder?: string;
  variant?: "filter" | "control" | "inline";
  className?: string;
  ariaLabel?: string;
}

export function OwnerPicker({
  value,
  valueDisplayName,
  valueEmail,
  onChange,
  allowClear = true,
  clearLabel,
  disabled = false,
  placeholder,
  variant = "filter",
  className,
  ariaLabel,
}: OwnerPickerProps) {
  const tFilters = useTranslations("crm.filters");
  const listboxId = useId();

  const [open, setOpen] = useState(false);
  const [searchInput, setSearchInput] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [prevDebouncedQ, setPrevDebouncedQ] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const [selectedSnapshot, setSelectedSnapshot] = useState<OwnerOption | null>(
    null,
  );
  const inputRef = useRef<HTMLInputElement>(null);

  if (debouncedQ !== prevDebouncedQ) {
    setPrevDebouncedQ(debouncedQ);
    setActiveIndex(0);
  }

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedQ(searchInput.trim());
    }, LIMITS.searchDebounceMs);

    return () => clearTimeout(timer);
  }, [searchInput]);

  const ownersQuery = useOwners(
    {
      q: debouncedQ || undefined,
      limit: LIMITS.ownerPickerLimit,
    },
    { enabled: open || Boolean(value) },
  );

  const owners = ownersQuery.data?.items ?? [];
  const matchedInPrimary = value ? owners.find((o) => o.id === value) : null;

  const lookupOwnersQuery = useOwners(
    {
      limit: LIMITS.ownerPickerMaxLimit,
    },
    {
      enabled:
        Boolean(value) &&
        !matchedInPrimary &&
        selectedSnapshot?.id !== value &&
        !valueDisplayName &&
        !valueEmail,
    },
  );
  const lookupOwners = lookupOwnersQuery.data?.items ?? [];
  const matchedInLookup = value
    ? lookupOwners.find((o) => o.id === value)
    : null;

  const discoveredOwner = matchedInPrimary ?? matchedInLookup ?? null;
  if (discoveredOwner && selectedSnapshot?.id !== discoveredOwner.id) {
    setSelectedSnapshot(discoveredOwner);
  }
  const clearOffset = allowClear ? 1 : 0;
  const totalOptions = owners.length + clearOffset;

  const activeOptionId =
    allowClear && activeIndex === 0
      ? `${listboxId}-option-clear`
      : owners[activeIndex - clearOffset]
        ? `${listboxId}-option-${owners[activeIndex - clearOffset].id}`
        : undefined;

  function handleSelect(ownerId: string | null, owner?: OwnerOption | null) {
    setSelectedSnapshot(owner ?? null);
    onChange(ownerId, owner ?? null);
    setOpen(false);
    setSearchInput("");
  }

  function handleKeyDown(e: KeyboardEvent) {
    if (totalOptions === 0) {
      if (e.key === "Escape") {
        e.preventDefault();
        setOpen(false);
      }
      return;
    }
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setActiveIndex((prev) => (prev + 1) % totalOptions);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIndex((prev) => (prev - 1 + totalOptions) % totalOptions);
    } else if (e.key === "Enter") {
      e.preventDefault();
      if (allowClear && activeIndex === 0) {
        handleSelect(null, null);
      } else {
        const item = owners[activeIndex - clearOffset];
        if (item) handleSelect(item.id, item);
      }
    } else if (e.key === "Escape") {
      e.preventDefault();
      setOpen(false);
    }
  }

  const matchedOwner = value
    ? (discoveredOwner ??
      (selectedSnapshot?.id === value ? selectedSnapshot : null))
    : null;

  const resolvedName =
    matchedOwner?.displayName ??
    valueDisplayName ??
    matchedOwner?.email ??
    valueEmail ??
    null;
  const resolvedEmail = matchedOwner?.email ?? valueEmail ?? null;
  const resolvedClearLabel = clearLabel ?? tFilters("allOwners");
  const triggerPlaceholder = placeholder ?? resolvedClearLabel;
  const triggerLabel = ariaLabel ?? tFilters("owner");
  const selectedDisplayLabel =
    resolvedName ?? (value ? tFilters("ownerPicker.selectedFallback") : null);

  if (disabled) {
    return (
      <span className={cn("text-xs text-muted-foreground", className)}>
        {selectedDisplayLabel || triggerPlaceholder}
      </span>
    );
  }

  const liveRegionStatus = ownersQuery.isFetching
    ? tFilters("ownerPicker.loading")
    : owners.length === 0 && debouncedQ
      ? tFilters("ownerPicker.empty")
      : tFilters("ownerPicker.resultsCount", { count: owners.length });

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger asChild>
        {variant === "inline" ? (
          <button
            type="button"
            aria-expanded={open}
            aria-haspopup="listbox"
            aria-controls={open ? listboxId : undefined}
            aria-label={triggerLabel}
            className={cn(
              "inline-flex items-center gap-1.5 rounded-[var(--radius-control)] px-1.5 py-0.5 text-xs font-medium text-left transition-colors hover:bg-muted/80 focus-visible:outline-2 focus-visible:outline-ring cursor-pointer group",
              selectedDisplayLabel
                ? "text-foreground"
                : "text-muted-foreground/80 italic",
              className,
            )}
          >
            <span className="truncate">
              {selectedDisplayLabel || triggerPlaceholder}
            </span>
            <ChevronsUpDown className="h-3 w-3 text-muted-foreground/60 opacity-0 group-hover:opacity-100 transition-opacity shrink-0" />
          </button>
        ) : (
          <button
            type="button"
            aria-expanded={open}
            aria-haspopup="listbox"
            aria-controls={open ? listboxId : undefined}
            aria-label={triggerLabel}
            className={cn(
              "flex h-8 items-center justify-between gap-2 rounded-[var(--radius-control)] border border-input bg-background px-2.5 py-1 text-xs shadow-xs focus-visible:outline-2 focus-visible:outline-ring cursor-pointer",
              variant === "filter"
                ? "w-auto min-w-[180px] max-w-[260px]"
                : "w-full",
              className,
            )}
          >
            <span
              className={cn(
                "truncate text-left",
                !value && variant === "control" && "text-muted-foreground",
              )}
            >
              {variant === "filter" ? (
                value && selectedDisplayLabel ? (
                  <>
                    <span className="text-muted-foreground">
                      {tFilters("owner")}:{" "}
                    </span>
                    <span className="font-medium text-foreground">
                      {selectedDisplayLabel}
                    </span>
                    {resolvedEmail &&
                      resolvedEmail !== selectedDisplayLabel && (
                        <span className="text-muted-foreground ml-1">
                          ({resolvedEmail})
                        </span>
                      )}
                  </>
                ) : (
                  tFilters("ownerFilter", { owner: resolvedClearLabel })
                )
              ) : value && selectedDisplayLabel ? (
                <span>
                  <span className="font-medium text-foreground">
                    {selectedDisplayLabel}
                  </span>
                  {resolvedEmail && resolvedEmail !== selectedDisplayLabel && (
                    <span className="text-muted-foreground ml-1">
                      ({resolvedEmail})
                    </span>
                  )}
                </span>
              ) : (
                triggerPlaceholder
              )}
            </span>
            <ChevronsUpDown className="h-3.5 w-3.5 text-muted-foreground shrink-0 opacity-70" />
          </button>
        )}
      </Popover.Trigger>

      <Popover.Portal>
        <Popover.Content
          align="start"
          sideOffset={4}
          onKeyDown={handleKeyDown}
          className="z-50 w-[var(--radix-popover-trigger-width)] min-w-[240px] rounded-[var(--radius-card)] border border-border bg-card p-1 text-foreground shadow-floating outline-none relative overflow-hidden"
        >
          <TableProgressBar active={ownersQuery.isFetching} />

          <div className="sr-only" role="status" aria-live="polite">
            {liveRegionStatus}
          </div>

          <div className="flex items-center border-b border-border px-2 py-1.5">
            <Search className="mr-2 h-3.5 w-3.5 shrink-0 text-muted-foreground" />
            <Input
              ref={inputRef}
              autoFocus
              role="combobox"
              aria-expanded={open}
              aria-haspopup="listbox"
              aria-controls={listboxId}
              aria-activedescendant={activeOptionId}
              aria-autocomplete="list"
              aria-label={tFilters("ownerPicker.searchPlaceholder")}
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder={tFilters("ownerPicker.searchPlaceholder")}
              className="h-7 border-0 bg-transparent px-1 text-xs shadow-none focus-visible:ring-0"
            />
            {searchInput && (
              <button
                type="button"
                onClick={() => setSearchInput("")}
                className="text-muted-foreground hover:text-foreground p-0.5 cursor-pointer"
                aria-label={tFilters("clearSearch")}
              >
                <X className="h-3 w-3" />
              </button>
            )}
          </div>

          <div
            id={listboxId}
            role="listbox"
            aria-label={tFilters("owner")}
            data-max-visible-rows={LIMITS.comboboxMaxVisibleRows}
            className="max-h-44 overflow-y-auto p-1 space-y-0.5"
          >
            {allowClear && (
              <button
                id={`${listboxId}-option-clear`}
                role="option"
                aria-selected={!value}
                type="button"
                onClick={() => handleSelect(null, null)}
                className={cn(
                  "flex w-full items-center justify-between rounded-[var(--radius-control)] px-2 py-1.5 text-xs text-muted-foreground hover:bg-muted hover:text-foreground cursor-pointer transition-colors",
                  activeIndex === 0 && "bg-muted text-foreground",
                )}
              >
                <span>{resolvedClearLabel}</span>
                {!value && (
                  <Check className="h-3.5 w-3.5 text-primary shrink-0" />
                )}
              </button>
            )}

            {owners.length === 0 && !ownersQuery.isFetching && (
              <div className="py-3 px-2 text-center text-xs text-muted-foreground">
                {tFilters("ownerPicker.empty")}
              </div>
            )}

            {owners.map((owner, idx) => {
              const isSelected = owner.id === value;
              const isHighlighted = activeIndex === idx + clearOffset;
              const optionId = `${listboxId}-option-${owner.id}`;
              const primaryLabel = owner.displayName || owner.email || owner.id;
              const secondaryEmail =
                owner.displayName && owner.email ? owner.email : null;

              return (
                <button
                  key={owner.id}
                  id={optionId}
                  role="option"
                  aria-selected={isSelected}
                  type="button"
                  onClick={() => handleSelect(owner.id, owner)}
                  className={cn(
                    "flex w-full items-center justify-between rounded-[var(--radius-control)] px-2 py-1.5 text-xs text-foreground hover:bg-muted cursor-pointer transition-colors text-left",
                    isHighlighted && "bg-muted",
                  )}
                >
                  <div className="flex items-center gap-2 min-w-0">
                    <User className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
                    <div className="flex flex-col min-w-0">
                      <span className="truncate font-medium">
                        {primaryLabel}
                      </span>
                      {secondaryEmail && (
                        <span className="font-mono text-[10px] text-muted-foreground truncate">
                          {secondaryEmail}
                        </span>
                      )}
                    </div>
                  </div>
                  {isSelected && (
                    <Check className="h-3.5 w-3.5 text-primary shrink-0 ml-1" />
                  )}
                </button>
              );
            })}
          </div>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
