"use client";

"use no memo"; // Opt out of React Compiler caching for TanStack Table instance mutations

import { ChevronLeft, ChevronRight } from "lucide-react";
import { useTranslations } from "next-intl";
import type { ReactNode } from "react";

import { Button } from "@/components/ui/button";
import { Select } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import { useDataTableContext } from "./data-table-context";

interface Props {
  paginationSlot?: ReactNode;
}

export function DataTablePagination({ paginationSlot }: Props) {
  const { table } = useDataTableContext();
  const t = useTranslations("table");

  if (paginationSlot) {
    return (
      <div className="flex flex-wrap items-center justify-between gap-4 py-3 text-xs text-muted-foreground border-t border-border">
        {paginationSlot}
      </div>
    );
  }

  const pageIndex = table.getState().pagination.pageIndex + 1;
  const pageCount = table.getPageCount() || 1;
  const pageSize = table.getState().pagination.pageSize;
  const selectedCount = table.getFilteredSelectedRowModel().rows.length;
  const totalCount = table.getFilteredRowModel().rows.length;

  return (
    <div className="flex flex-wrap items-center justify-between gap-4 py-3 text-xs text-muted-foreground border-t border-border">
      {/* Selection info */}
      <div className="text-xs">
        {selectedCount > 0 ? (
          <span>
            {t("selection.selected", {
              count: selectedCount,
              total: totalCount,
            })}
          </span>
        ) : (
          <span>{t("totalRecords", { count: totalCount })}</span>
        )}
      </div>

      <div className="flex items-center gap-4">
        {/* Page size select */}
        <div className="flex items-center gap-2">
          <span>{t("pagination.rowsPerPage")}:</span>
          <Select
            value={pageSize}
            onChange={(e) => table.setPageSize(Number(e.target.value))}
            className="h-7 w-16 px-1.5 py-0 text-xs bg-background"
          >
            {LIMITS.tablePageSizes.map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </Select>
        </div>

        {/* Page status */}
        <div className="font-mono">
          {t("pagination.page", { page: pageIndex, pages: pageCount })}
        </div>

        {/* Previous / Next buttons */}
        <div className="flex items-center gap-1">
          <Button
            variant="secondary"
            onClick={() => table.previousPage()}
            disabled={!table.getCanPreviousPage()}
            className="h-7 w-7 p-0"
            aria-label={t("pagination.previous")}
          >
            <ChevronLeft className="h-3.5 w-3.5" />
          </Button>
          <Button
            variant="secondary"
            onClick={() => table.nextPage()}
            disabled={!table.getCanNextPage()}
            className="h-7 w-7 p-0"
            aria-label={t("pagination.next")}
          >
            <ChevronRight className="h-3.5 w-3.5" />
          </Button>
        </div>
      </div>
    </div>
  );
}
