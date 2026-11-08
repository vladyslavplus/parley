"use client";

"use no memo"; // Opt out of React Compiler caching for TanStack Table instance mutations

import { Search } from "lucide-react";
import { useTranslations } from "next-intl";
import type { ReactNode } from "react";

import { Input } from "@/components/ui/input";
import { DataTableColumnsMenu } from "./data-table-columns-menu";
import { useDataTableContext } from "./data-table-context";
import { DataTableDensityToggle } from "./data-table-density-toggle";

interface DataTableToolbarProps {
  children?: ReactNode;
}

export function DataTableToolbar({ children }: DataTableToolbarProps) {
  const { searchValue, onSearchChange } = useDataTableContext();
  const t = useTranslations("table");

  return (
    <div className="flex flex-wrap items-center justify-between gap-3 py-3">
      {/* Search Input */}
      <div className="relative flex-1 max-w-sm">
        <Search className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
        <Input
          value={searchValue}
          onChange={(e) => onSearchChange(e.target.value)}
          placeholder={t("search")}
          className="h-8 pl-8 text-xs bg-background"
        />
      </div>

      <div className="flex items-center gap-2">
        {children ?? (
          <>
            <DataTableDensityToggle />
            <DataTableColumnsMenu />
          </>
        )}
      </div>
    </div>
  );
}
