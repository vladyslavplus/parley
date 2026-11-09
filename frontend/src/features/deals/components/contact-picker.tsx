"use client";

import * as Popover from "@radix-ui/react-popover";
import { Check, ChevronsUpDown, User, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";

import { TableProgressBar } from "@/components/data-table/table-progress-bar";
import { PICKER_TRIGGER } from "@/components/ui/input";
import { PickerSearch } from "@/components/ui/picker-search";
import { LIMITS } from "@/constants/limits";
import { useContacts } from "@/features/crm/hooks/use-contacts";
import { cn } from "@/lib/utils";

interface Props {
  value?: string | null;
  valueName?: string | null;
  companyId?: string | null;
  onChange: (contactId: string | null, contactName?: string | null) => void;
  disabled?: boolean;
  placeholder?: string;
  variant?: "inline" | "control";
  className?: string;
  ariaLabel?: string;
}

export function ContactPicker({
  value,
  valueName,
  companyId,
  onChange,
  disabled = false,
  placeholder,
  variant = "control",
  className,
  ariaLabel,
}: Props) {
  const t = useTranslations("crm.contacts");
  const tFilters = useTranslations("crm.filters");
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

  const contactsQuery = useContacts(
    {
      q: debouncedQ || undefined,
      companyId: companyId || undefined,
      limit: LIMITS.contactPickerLimit,
      sort: "name",
      order: "asc",
    },
    { enabled: open },
  );

  const contacts = contactsQuery.data?.items ?? [];

  function handleSelect(id: string, name: string) {
    onChange(id, name);
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
      setActiveIndex((prev) => (prev < contacts.length - 1 ? prev + 1 : prev));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIndex((prev) => (prev > 0 ? prev - 1 : 0));
    } else if (e.key === "Enter") {
      e.preventDefault();
      const current = contacts[activeIndex];
      if (current) {
        const name = [current.firstName, current.lastName]
          .filter(Boolean)
          .join(" ");
        handleSelect(current.id, name);
      }
    }
  }

  const selectedDisplay =
    valueName ?? (value ? t("record.fields.assigned") : null);

  return (
    <Popover.Root open={open} onOpenChange={setOpen}>
      <Popover.Trigger asChild>
        <button
          type="button"
          disabled={disabled}
          aria-label={
            ariaLabel ?? placeholder ?? t("record.fields.linkedCompany")
          }
          aria-expanded={open}
          aria-haspopup="listbox"
          onKeyDown={handleKeyDown}
          className={cn(
            "group flex items-center justify-between transition-colors",
            variant === "inline"
              ? "rounded-[var(--radius-control)] px-2 py-1 -mx-2 -my-1 text-sm hover:bg-muted/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
              : PICKER_TRIGGER,
            className,
          )}
        >
          <div className="flex items-center gap-2 truncate">
            <User className="h-4 w-4 shrink-0 text-muted-foreground" />
            {selectedDisplay ? (
              <span className="truncate text-foreground font-medium">
                {selectedDisplay}
              </span>
            ) : (
              <span className="truncate text-muted-foreground">
                {placeholder ?? t("searchPlaceholder")}
              </span>
            )}
          </div>
          <div className="flex items-center gap-1 shrink-0 ml-1">
            {value && !disabled && (
              <span
                role="button"
                tabIndex={0}
                onClick={handleClear}
                onKeyDown={(e) => {
                  if (e.key === "Enter" || e.key === " ") {
                    e.preventDefault();
                    onChange(null, null);
                  }
                }}
                aria-label={t("record.delete")}
                className="rounded-xs p-0.5 text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              >
                <X className="h-3.5 w-3.5" />
              </span>
            )}
            <ChevronsUpDown className="h-3.5 w-3.5 text-muted-foreground opacity-50 group-hover:opacity-100" />
          </div>
        </button>
      </Popover.Trigger>

      <Popover.Portal>
        <Popover.Content
          align="start"
          sideOffset={4}
          onKeyDown={handleKeyDown}
          className="z-50 w-[var(--radix-popover-trigger-width)] min-w-64 rounded-[var(--radius-card)] border border-border bg-popover p-1 text-popover-foreground shadow-[var(--shadow-floating)] focus-visible:outline-none"
        >
          <PickerSearch
            inputRef={inputRef}
            autoFocus
            value={searchInput}
            onValueChange={setSearchInput}
            placeholder={t("searchPlaceholder")}
            aria-label={t("searchPlaceholder")}
            clearLabel={tFilters("clearSearch")}
          />

          <TableProgressBar
            active={contactsQuery.isFetching && Boolean(contactsQuery.data)}
          />

          <div
            id={listboxId}
            role="listbox"
            tabIndex={-1}
            className="max-h-56 overflow-y-auto space-y-0.5"
          >
            {contacts.length === 0 ? (
              <div className="py-4 text-center text-xs text-muted-foreground">
                {contactsQuery.isPending ? t("empty.title") : t("empty.title")}
              </div>
            ) : (
              contacts.map((contact, index) => {
                const isSelected = contact.id === value;
                const isFocused = index === activeIndex;
                const contactFullName = [contact.firstName, contact.lastName]
                  .filter(Boolean)
                  .join(" ");

                return (
                  <div
                    key={contact.id}
                    role="option"
                    aria-selected={isSelected}
                    onClick={() => handleSelect(contact.id, contactFullName)}
                    onMouseEnter={() => setActiveIndex(index)}
                    className={cn(
                      "flex items-center justify-between rounded-[var(--radius-control)] px-2 py-1.5 text-xs cursor-pointer select-none transition-colors",
                      isFocused
                        ? "bg-accent text-accent-foreground"
                        : "text-foreground hover:bg-muted/50",
                      isSelected && "font-medium",
                    )}
                  >
                    <div className="truncate">
                      <div>{contactFullName}</div>
                      {contact.email && (
                        <div className="text-[10px] text-muted-foreground truncate">
                          {contact.email}
                        </div>
                      )}
                    </div>
                    {isSelected && (
                      <Check className="h-3.5 w-3.5 text-primary shrink-0 ml-2" />
                    )}
                  </div>
                );
              })
            )}
          </div>
        </Popover.Content>
      </Popover.Portal>
    </Popover.Root>
  );
}
