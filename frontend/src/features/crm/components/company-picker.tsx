"use client";

import * as Popover from "@radix-ui/react-popover";
import { Building2, Check, ChevronsUpDown, Search, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";

import { TableProgressBar } from "@/components/data-table/table-progress-bar";
import { Input } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import { useCompanies } from "@/features/crm/hooks/use-companies";
import { cn } from "@/lib/utils";

interface Props {
  value?: string | null;
  valueName?: string | null;
  onChange: (companyId: string | null, companyName?: string | null) => void;
  disabled?: boolean;
  placeholder?: string;
  variant?: "inline" | "control";
  className?: string;
  ariaLabel?: string;
}

export function CompanyPicker({
  value,
  valueName,
  onChange,
  disabled = false,
  placeholder,
  variant = "control",
  className,
  ariaLabel,
}: Props) {
  const t = useTranslations("crm.companies");
  const listboxId = useId();

  const [open, setOpen] = useState(false);
  const [searchInput, setSearchInput] = useState("");
  const [debouncedQ, setDebouncedQ] = useState("");
  const [prevDebouncedQ, setPrevDebouncedQ] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const inputRef = useRef<HTMLInputElement>(null);

  if (debouncedQ !== prevDebouncedQ) {
    setPrevDebouncedQ(debouncedQ);
    setActiveIndex(0);
  }

  // Debounce search input
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedQ(searchInput.trim());
    }, LIMITS.searchDebounceMs);

    return () => clearTimeout(timer);
  }, [searchInput]);

  const companiesQuery = useCompanies(
    {
      q: debouncedQ || undefined,
      limit: LIMITS.companyPickerLimit,
    },
    { enabled: open },
  );

  const companies = companiesQuery.data?.items ?? [];
  const totalOptions = companies.length + 1; // +1 for "No company" option

  const activeOptionId =
    activeIndex === 0
      ? `${listboxId}-option-none`
      : companies[activeIndex - 1]
        ? `${listboxId}-option-${companies[activeIndex - 1].id}`
        : undefined;

  function handleSelect(companyId: string | null, companyName?: string | null) {
    onChange(companyId, companyName);
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
        const item = companies[activeIndex - 1];
        if (item) handleSelect(item.id, item.name);
      }
    } else if (e.key === "Escape") {
      e.preventDefault();
      setOpen(false);
    }
  }

  const selectedDisplay =
    valueName ?? (value ? companies.find((c) => c.id === value)?.name : null);

  const triggerPlaceholder = placeholder ?? t("picker.noCompany");
  const triggerLabel = ariaLabel ?? placeholder ?? t("picker.placeholder");

  if (disabled) {
    return (
      <span className={cn("text-xs text-muted-foreground", className)}>
        {selectedDisplay || triggerPlaceholder}
      </span>
    );
  }

  const liveRegionStatus = companiesQuery.isFetching
    ? t("picker.loading")
    : companies.length === 0 && debouncedQ
      ? t("picker.empty")
      : t("picker.resultsCount", { count: companies.length });

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
              selectedDisplay
                ? "text-info hover:underline"
                : "text-muted-foreground/80 italic",
              className,
            )}
          >
            <span>{selectedDisplay || triggerPlaceholder}</span>
            <ChevronsUpDown className="h-3 w-3 text-muted-foreground/60 opacity-0 group-hover:opacity-100 transition-opacity" />
          </button>
        ) : (
          <button
            type="button"
            aria-expanded={open}
            aria-haspopup="listbox"
            aria-controls={open ? listboxId : undefined}
            aria-label={triggerLabel}
            className={cn(
              "flex h-8 w-full items-center justify-between rounded-[var(--radius-control)] border border-input bg-background px-3 py-1 text-xs shadow-xs focus-visible:outline-2 focus-visible:outline-ring cursor-pointer",
              className,
            )}
          >
            <span
              className={cn(
                "truncate",
                !selectedDisplay && "text-muted-foreground",
              )}
            >
              {selectedDisplay || triggerPlaceholder}
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
          className="z-50 w-[var(--radix-popover-trigger-width)] min-w-[200px] rounded-[var(--radius-card)] border border-border bg-card p-1 text-foreground shadow-floating outline-none relative overflow-hidden"
        >
          <TableProgressBar active={companiesQuery.isFetching} />

          {/* Accessible live region for search results */}
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
              aria-label={t("picker.searchPlaceholder")}
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              placeholder={t("picker.searchPlaceholder")}
              className="h-7 border-0 bg-transparent px-1 text-xs shadow-none focus-visible:ring-0"
            />
            {searchInput && (
              <button
                type="button"
                onClick={() => setSearchInput("")}
                className="text-muted-foreground hover:text-foreground p-0.5"
                aria-label={t("picker.clear")}
              >
                <X className="h-3 w-3" />
              </button>
            )}
          </div>

          <div
            id={listboxId}
            role="listbox"
            aria-label={t("picker.title")}
            className="max-h-56 overflow-y-auto p-1 space-y-0.5"
          >
            {/* Clear option */}
            <button
              id={`${listboxId}-option-none`}
              role="option"
              aria-selected={!value}
              type="button"
              onClick={() => handleSelect(null, null)}
              className={cn(
                "flex w-full items-center justify-between rounded-[var(--radius-control)] px-2 py-1.5 text-xs text-muted-foreground hover:bg-muted hover:text-foreground cursor-pointer transition-colors",
                activeIndex === 0 && "bg-muted text-foreground",
              )}
            >
              <span className="italic">{t("picker.noCompany")}</span>
              {!value && <Check className="h-3.5 w-3.5 text-primary" />}
            </button>

            {companies.length === 0 &&
              debouncedQ &&
              !companiesQuery.isFetching && (
                <div className="py-4 text-center text-xs text-muted-foreground">
                  {t("picker.empty")}
                </div>
              )}

            {companies.map((company, idx) => {
              const isSelected = company.id === value;
              const isHighlighted = activeIndex === idx + 1;
              const optionId = `${listboxId}-option-${company.id}`;

              return (
                <button
                  key={company.id}
                  id={optionId}
                  role="option"
                  aria-selected={isSelected}
                  type="button"
                  onClick={() => handleSelect(company.id, company.name)}
                  className={cn(
                    "flex w-full items-center justify-between rounded-[var(--radius-control)] px-2 py-1.5 text-xs text-foreground hover:bg-muted cursor-pointer transition-colors text-left",
                    isHighlighted && "bg-muted",
                  )}
                >
                  <div className="flex items-center gap-2 truncate">
                    <Building2 className="h-3.5 w-3.5 text-muted-foreground shrink-0" />
                    <div className="flex flex-col truncate">
                      <span className="truncate font-medium">
                        {company.name}
                      </span>
                      {company.domain && (
                        <span className="text-[10px] text-muted-foreground truncate">
                          {company.domain}
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
