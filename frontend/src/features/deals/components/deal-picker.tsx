"use client";

import * as Popover from "@radix-ui/react-popover";
import { Check, ChevronsUpDown, Kanban, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";

import { TableProgressBar } from "@/components/data-table/table-progress-bar";
import { PICKER_TRIGGER } from "@/components/ui/input";
import { PickerSearch } from "@/components/ui/picker-search";
import { LIMITS } from "@/constants/limits";
import { useDeals } from "@/features/deals/hooks/use-deals";
import { cn } from "@/lib/utils";

interface Props {
  value?: string | null;
  valueTitle?: string | null;
  companyId?: string | null;
  contactId?: string | null;
  onChange: (dealId: string | null, dealTitle?: string | null) => void;
  disabled?: boolean;
  placeholder?: string;
  variant?: "inline" | "control";
  className?: string;
  ariaLabel?: string;
}

export function DealPicker({
  value,
  valueTitle,
  companyId,
  contactId,
  onChange,
  disabled = false,
  placeholder,
  variant = "control",
  className,
  ariaLabel,
}: Props) {
  const t = useTranslations("crm.deals");
  const tCrm = useTranslations("crm");
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

  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedQ(searchInput.trim());
    }, LIMITS.searchDebounceMs);

    return () => clearTimeout(timer);
  }, [searchInput]);

  const dealsQuery = useDeals(
    {
      q: debouncedQ || undefined,
      companyId: companyId || undefined,
      contactId: contactId || undefined,
      limit: LIMITS.dealPickerLimit,
      sort: "name",
      order: "asc",
    },
    { enabled: open },
  );

  const deals = dealsQuery.data?.items ?? [];

  function handleSelect(id: string, title: string) {
    onChange(id, title);
    setOpen(false);
    setSearchInput("");
  }

  function handleClear(e: React.MouseEvent) {
    e.stopPropagation();
    onChange(null, null);
  }

  function handleKeyDown(e: KeyboardEvent<HTMLElement>) {
    if (!open) {
      if (e.key === "ArrowDown" || e.key === "Enter" || e.key === " ") {
        e.preventDefault();
        setOpen(true);
      }
      return;
    }

    if (e.key === "Escape") {
      e.preventDefault();
      setOpen(false);
      return;
    }

    if (e.key === "ArrowDown") {
      e.preventDefault();
      setActiveIndex((prev) => (prev < deals.length - 1 ? prev + 1 : prev));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIndex((prev) => (prev > 0 ? prev - 1 : 0));
    } else if (e.key === "Enter") {
      e.preventDefault();
      const current = deals[activeIndex];
      if (current) {
        handleSelect(current.id, current.title);
      }
    }
  }

  const selectedDisplay = valueTitle ?? (value ? t("title") : null);

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger asChild>
        <button
          type="button"
          disabled={disabled}
          onKeyDown={handleKeyDown}
          aria-haspopup="listbox"
          aria-expanded={open}
          aria-controls={open ? listboxId : undefined}
          aria-label={ariaLabel ?? placeholder ?? t("title")}
          className={cn(
            "group inline-flex items-center justify-between text-left transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring",
            variant === "inline"
              ? "h-7 gap-1.5 rounded-xs px-1 text-xs hover:bg-muted text-foreground font-medium"
              : PICKER_TRIGGER,
            disabled && "cursor-not-allowed opacity-50",
            className,
          )}
        >
          <span className="flex items-center gap-1.5 truncate">
            <Kanban className="h-4 w-4 shrink-0 text-muted-foreground" />
            <span
              className={cn(
                "truncate",
                !selectedDisplay && "text-muted-foreground",
              )}
            >
              {selectedDisplay || placeholder || t("title")}
            </span>
          </span>

          <span className="flex items-center gap-1 shrink-0 ml-1">
            {value && !disabled && (
              <span
                role="button"
                tabIndex={0}
                onClick={handleClear}
                onKeyDown={(e) => {
                  if (e.key === "Enter" || e.key === " ") {
                    handleClear(e as unknown as React.MouseEvent);
                  }
                }}
                aria-label={t("picker.clear")}
                className="rounded-xs p-0.5 text-muted-foreground hover:text-foreground"
              >
                <X className="h-3 w-3" />
              </span>
            )}
            <ChevronsUpDown className="h-3 w-3 text-muted-foreground" />
          </span>
        </button>
      </Popover.Trigger>

      <Popover.Portal>
        <Popover.Content
          align="start"
          sideOffset={4}
          onKeyDown={handleKeyDown}
          className="z-50 w-[var(--radix-popover-trigger-width)] min-w-64 rounded-[var(--radius-card)] border border-border bg-popover p-1 shadow-[var(--shadow-floating)] outline-none"
        >
          <PickerSearch
            inputRef={inputRef}
            autoFocus
            value={searchInput}
            onValueChange={setSearchInput}
            placeholder={t("toolbar.searchPlaceholder")}
            aria-label={t("toolbar.searchPlaceholder")}
            clearLabel={tCrm("filters.clearSearch")}
          />

          <TableProgressBar active={dealsQuery.isFetching} />

          <ul
            id={listboxId}
            role="listbox"
            tabIndex={-1}
            className="max-h-48 overflow-y-auto space-y-0.5 p-0.5 text-xs"
          >
            {dealsQuery.isLoading ? (
              <li className="px-2 py-3 text-center text-xs text-muted-foreground">
                {t("column.empty")}
              </li>
            ) : deals.length === 0 ? (
              <li className="px-2 py-3 text-center text-xs text-muted-foreground">
                {t("column.empty")}
              </li>
            ) : (
              deals.map((deal, idx) => {
                const isSelected = deal.id === value;
                const isActive = idx === activeIndex;

                return (
                  <li
                    key={deal.id}
                    role="option"
                    aria-selected={isSelected}
                    onClick={() => handleSelect(deal.id, deal.title)}
                    onMouseEnter={() => setActiveIndex(idx)}
                    className={cn(
                      "flex cursor-pointer items-center justify-between rounded-xs px-2 py-1.5 transition-colors",
                      isActive && "bg-muted text-foreground",
                      isSelected && "font-semibold text-foreground",
                    )}
                  >
                    <div className="flex items-center gap-2 truncate">
                      <Kanban className="h-3 w-3 shrink-0 text-muted-foreground" />
                      <span className="truncate">{deal.title}</span>
                    </div>
                    {isSelected && (
                      <Check className="h-3.5 w-3.5 shrink-0 text-ring" />
                    )}
                  </li>
                );
              })
            )}
          </ul>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
