"use client";

import * as Popover from "@radix-ui/react-popover";
import {
  Check,
  ChevronsUpDown,
  Info,
  Search,
  Tag as TagIcon,
  X,
} from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";

import { TableProgressBar } from "@/components/data-table/table-progress-bar";
import { Input } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import { CrmTagBadge } from "@/features/crm/components/crm-tag-badge";
import { useTags } from "@/features/crm/hooks/use-tags";
import type { Tag } from "@/features/crm/types";
import { cn } from "@/lib/utils";

export interface TagPickerProps {
  value?: string | null;
  valueName?: string | null;
  onChange: (tagId: string | null, tag?: Tag | null) => void;
  disabled?: boolean;
  className?: string;
  ariaLabel?: string;
}

export function TagPicker({
  value,
  valueName,
  onChange,
  disabled = false,
  className,
  ariaLabel,
}: TagPickerProps) {
  const tFilters = useTranslations("crm.filters");
  const listboxId = useId();
  const helperId = useId();

  const [open, setOpen] = useState(false);
  const [searchInput, setSearchInput] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [prevDebouncedQ, setPrevDebouncedQ] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const [selectedSnapshot, setSelectedSnapshot] = useState<Tag | null>(null);
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

  const tagsQuery = useTags({
    q: debouncedQ || undefined,
    limit: LIMITS.tagPickerLimit,
    enabled: open || Boolean(value),
  });

  const tags = tagsQuery.data?.items ?? [];
  const matchedInPrimary = value ? tags.find((tg) => tg.id === value) : null;

  const lookupTagsQuery = useTags({
    limit: LIMITS.crmLookupLimit,
    enabled:
      Boolean(value) &&
      !matchedInPrimary &&
      selectedSnapshot?.id !== value &&
      !valueName,
  });
  const lookupTags = lookupTagsQuery.data?.items ?? [];
  const matchedInLookup = value
    ? lookupTags.find((tg) => tg.id === value)
    : null;

  const discoveredTag = matchedInPrimary ?? matchedInLookup ?? null;
  if (discoveredTag && selectedSnapshot?.id !== discoveredTag.id) {
    setSelectedSnapshot(discoveredTag);
  }
  const totalOptions = tags.length + 1;

  const activeOptionId =
    activeIndex === 0
      ? `${listboxId}-option-all`
      : tags[activeIndex - 1]
        ? `${listboxId}-option-${tags[activeIndex - 1].id}`
        : undefined;

  function handleSelect(tagId: string | null, tag?: Tag | null) {
    setSelectedSnapshot(tag ?? null);
    onChange(tagId, tag ?? null);
    setOpen(false);
    setSearchInput("");
  }

  function handleKeyDown(e: KeyboardEvent) {
    if (e.key === "ArrowDown") {
      e.preventDefault();
      setActiveIndex((prev) => (prev + 1) % totalOptions);
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIndex((prev) => (prev - 1 + totalOptions) % totalOptions);
    } else if (e.key === "Enter") {
      e.preventDefault();
      if (activeIndex === 0) {
        handleSelect(null, null);
      } else {
        const item = tags[activeIndex - 1];
        if (item) handleSelect(item.id, item);
      }
    } else if (e.key === "Escape") {
      e.preventDefault();
      setOpen(false);
    }
  }

  const matchedTag = value
    ? (discoveredTag ??
      (selectedSnapshot?.id === value ? selectedSnapshot : null))
    : null;

  const resolvedTagName =
    matchedTag?.name ??
    valueName ??
    (value ? tFilters("tagPicker.selectedFallback") : null);

  const triggerLabel = ariaLabel ?? tFilters("tag");

  if (disabled) {
    return (
      <span className={cn("text-xs text-muted-foreground", className)}>
        {resolvedTagName || tFilters("allTags")}
      </span>
    );
  }

  const liveRegionStatus = tagsQuery.isFetching
    ? tFilters("tagPicker.loading")
    : tags.length === 0
      ? tFilters("tagPicker.empty")
      : tFilters("tagPicker.resultsCount", { count: tags.length });

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger asChild>
        <button
          type="button"
          aria-expanded={open}
          aria-haspopup="listbox"
          aria-controls={open ? listboxId : undefined}
          aria-label={triggerLabel}
          title={tFilters("tagPicker.help")}
          className={cn(
            "flex h-8 w-auto min-w-[160px] max-w-[240px] items-center justify-between gap-2 rounded-[var(--radius-control)] border border-input bg-background px-2.5 py-1 text-xs shadow-xs focus-visible:outline-2 focus-visible:outline-ring cursor-pointer",
            className,
          )}
        >
          <span className="truncate flex items-center gap-1.5 text-left">
            <TagIcon className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
            {value && resolvedTagName ? (
              <span>
                <span className="text-muted-foreground">
                  {tFilters("tag")}:{" "}
                </span>
                <span className="font-medium text-foreground">
                  {resolvedTagName}
                </span>
              </span>
            ) : (
              <span>{tFilters("allTags")}</span>
            )}
          </span>
          <ChevronsUpDown className="h-3.5 w-3.5 text-muted-foreground shrink-0 opacity-70" />
        </button>
      </Popover.Trigger>

      <Popover.Portal>
        <Popover.Content
          align="start"
          sideOffset={4}
          onKeyDown={handleKeyDown}
          aria-describedby={helperId}
          className="z-50 w-[var(--radix-popover-trigger-width)] min-w-[250px] rounded-[var(--radius-card)] border border-border bg-card p-1 text-foreground shadow-floating outline-none relative overflow-hidden"
        >
          <TableProgressBar active={tagsQuery.isFetching} />

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
              aria-label={tFilters("tagPicker.searchPlaceholder")}
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder={tFilters("tagPicker.searchPlaceholder")}
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
            aria-label={tFilters("tag")}
            data-max-visible-rows={LIMITS.comboboxMaxVisibleRows}
            className="max-h-44 overflow-y-auto p-1 space-y-0.5"
          >
            <button
              id={`${listboxId}-option-all`}
              role="option"
              aria-selected={!value}
              type="button"
              onClick={() => handleSelect(null, null)}
              className={cn(
                "flex w-full items-center justify-between rounded-[var(--radius-control)] px-2 py-1.5 text-xs text-muted-foreground hover:bg-muted hover:text-foreground cursor-pointer transition-colors",
                activeIndex === 0 && "bg-muted text-foreground",
              )}
            >
              <span>{tFilters("allTags")}</span>
              {!value && (
                <Check className="h-3.5 w-3.5 text-primary shrink-0" />
              )}
            </button>

            {tags.length === 0 && !tagsQuery.isFetching && (
              <div className="py-3 px-2 text-center text-xs text-muted-foreground">
                {debouncedQ
                  ? tFilters("tagPicker.emptySearch")
                  : tFilters("tagPicker.empty")}
              </div>
            )}

            {tags.map((tg, idx) => {
              const isSelected = tg.id === value;
              const isHighlighted = activeIndex === idx + 1;
              const optionId = `${listboxId}-option-${tg.id}`;

              return (
                <button
                  key={tg.id}
                  id={optionId}
                  role="option"
                  aria-selected={isSelected}
                  type="button"
                  onClick={() => handleSelect(tg.id, tg)}
                  className={cn(
                    "flex w-full items-center justify-between rounded-[var(--radius-control)] px-2 py-1.5 text-xs text-foreground hover:bg-muted cursor-pointer transition-colors text-left",
                    isHighlighted && "bg-muted",
                  )}
                >
                  <CrmTagBadge name={tg.name} colorIndex={tg.colorIndex} />
                  {isSelected && (
                    <Check className="h-3.5 w-3.5 text-primary shrink-0 ml-1" />
                  )}
                </button>
              );
            })}
          </div>

          {/* Short helper text explaining what a tag is */}
          <div
            id={helperId}
            className="flex items-start gap-1.5 border-t border-border/60 bg-muted/30 px-2.5 py-2 text-[11px] leading-snug text-muted-foreground"
          >
            <Info className="h-3.5 w-3.5 shrink-0 mt-0.5 text-muted-foreground" />
            <span>{tFilters("tagPicker.help")}</span>
          </div>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
