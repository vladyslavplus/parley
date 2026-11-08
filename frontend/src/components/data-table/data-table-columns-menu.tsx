"use client";

"use no memo"; // Opt out of React Compiler caching for TanStack Table instance mutations

import * as DropdownMenu from "@radix-ui/react-dropdown-menu";
import { Check, Columns } from "lucide-react";
import { useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { useDataTableContext } from "./data-table-context";

export function DataTableColumnsMenu() {
  const { table } = useDataTableContext();
  const t = useTranslations("table");

  const toggleableColumns = table
    .getAllColumns()
    .filter((col) => col.getCanHide());

  if (toggleableColumns.length === 0) return null;

  return (
    <DropdownMenu.Root>
      <DropdownMenu.Trigger asChild>
        <Button variant="secondary" className="h-8 px-2.5 text-xs gap-1.5">
          <Columns className="h-3.5 w-3.5 text-muted-foreground" />
          <span>{t("columns")}</span>
        </Button>
      </DropdownMenu.Trigger>

      <DropdownMenu.Portal>
        <DropdownMenu.Content
          align="end"
          className="z-50 min-w-44 rounded-[var(--radius-card)] border border-border bg-card p-1 shadow-floating"
        >
          <DropdownMenu.Label className="px-2 py-1.5 text-xs font-semibold text-muted-foreground">
            {t("columns")}
          </DropdownMenu.Label>
          <DropdownMenu.Separator className="my-1 h-px bg-border" />
          <div className="max-h-56 overflow-y-auto space-y-0.5">
            {toggleableColumns.map((column) => {
              const meta = column.columnDef.meta as
                { label?: string } | undefined;
              const header = column.columnDef.header;
              const label =
                meta?.label ??
                (typeof header === "string" ? header : column.id);

              return (
                <DropdownMenu.CheckboxItem
                  key={column.id}
                  checked={column.getIsVisible()}
                  onCheckedChange={(checked) =>
                    column.toggleVisibility(Boolean(checked))
                  }
                  className="relative flex select-none items-center rounded-sm py-1.5 pl-7 pr-2 text-xs text-foreground outline-none transition-colors hover:bg-muted focus:bg-muted cursor-pointer"
                >
                  <span className="absolute left-2 flex h-3.5 w-3.5 items-center justify-center">
                    <DropdownMenu.ItemIndicator>
                      <Check className="h-3.5 w-3.5 text-primary" />
                    </DropdownMenu.ItemIndicator>
                  </span>
                  <span className="capitalize">{label}</span>
                </DropdownMenu.CheckboxItem>
              );
            })}
          </div>
        </DropdownMenu.Content>
      </DropdownMenu.Portal>
    </DropdownMenu.Root>
  );
}
