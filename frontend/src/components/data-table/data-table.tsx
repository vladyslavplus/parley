"use client";

"use no memo"; // Opt out of React Compiler caching for TanStack Table instance mutations

import {
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel,
  useReactTable,
  type PaginationState,
  type RowSelectionState,
  type SortingState,
  type VisibilityState,
} from "@tanstack/react-table";
import { ArrowDown, ArrowUp, ArrowUpDown } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useRef, useState, type KeyboardEvent } from "react";

import { EmptyState } from "@/components/ui/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import { LIMITS } from "@/constants/limits";
import { cn } from "@/lib/utils";
import { DataTableColumnsMenu } from "./data-table-columns-menu";
import { DataTableProvider } from "./data-table-context";
import { DataTableDensityToggle } from "./data-table-density-toggle";
import { DataTablePagination } from "./data-table-pagination";
import { DataTableToolbar } from "./data-table-toolbar";
import { TableProgressBar } from "./table-progress-bar";
import type { DataTableProps, TableDensity } from "./types";

export function DataTable<TData>({
  columns,
  data,
  getRowId,
  loading = false,
  isFetching = false,
  emptyTitle,
  emptyDescription,
  emptyAction,
  onRowClick,
  bulkActions,
  initialPageSize = LIMITS.defaultTablePageSize,
  sorting: controlledSorting,
  onSortingChange: setControlledSorting,
  pagination: controlledPagination,
  onPaginationChange: setControlledPagination,
  globalFilter: controlledGlobalFilter,
  onGlobalFilterChange: setControlledGlobalFilter,
  manualPagination = false,
  manualSorting = false,
  pageCount,
  paginationSlot,
  toolbar,
  className,
}: DataTableProps<TData>) {
  const t = useTranslations("table");

  // Internal states when not controlled externally
  const [internalSearch, setInternalSearch] = useState("");
  const [internalSorting, setInternalSorting] = useState<SortingState>([]);
  const [internalPagination, setInternalPagination] = useState<PaginationState>(
    {
      pageIndex: 0,
      pageSize: initialPageSize,
    },
  );
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({});
  const [rowSelection, setRowSelection] = useState<RowSelectionState>({});
  const [density, setDensity] = useState<TableDensity>("compact");
  const [activeIndex, setActiveIndex] = useState<number>(-1);

  const tableRef = useRef<HTMLTableElement>(null);
  const rowElementsRef = useRef<(HTMLTableRowElement | null)[]>([]);

  const searchValue = controlledGlobalFilter ?? internalSearch;
  const onSearchChange = setControlledGlobalFilter ?? setInternalSearch;
  const activeSorting = controlledSorting ?? internalSorting;
  const onSortingChange = setControlledSorting ?? setInternalSorting;
  const activePagination = controlledPagination ?? internalPagination;
  const onPaginationChange = setControlledPagination ?? setInternalPagination;

  // eslint-disable-next-line react-hooks/incompatible-library
  const table = useReactTable({
    data,
    columns,
    getRowId,
    state: {
      sorting: activeSorting,
      pagination: activePagination,
      globalFilter: searchValue,
      columnVisibility,
      rowSelection,
    },
    manualPagination,
    manualSorting,
    pageCount: manualPagination ? pageCount : undefined,
    enableRowSelection: true,
    onRowSelectionChange: setRowSelection,
    onSortingChange,
    onPaginationChange,
    onGlobalFilterChange: onSearchChange,
    onColumnVisibilityChange: setColumnVisibility,
    getCoreRowModel: getCoreRowModel(),
    getFilteredRowModel: getFilteredRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    getSortedRowModel: getSortedRowModel(),
  });

  const rows = table.getRowModel().rows;
  const selectedRows = table
    .getFilteredSelectedRowModel()
    .rows.map((r) => r.original);
  const totalFilteredCount = table.getFilteredRowModel().rows.length;

  const pageIndex = activePagination.pageIndex;

  // Reset activeIndex when page or filter changes
  useEffect(() => {
    setActiveIndex(-1);
  }, [searchValue, pageIndex]);

  // Scroll active row into view
  useEffect(() => {
    if (activeIndex >= 0 && rowElementsRef.current[activeIndex]) {
      rowElementsRef.current[activeIndex]?.scrollIntoView?.({
        block: "nearest",
      });
    }
  }, [activeIndex]);

  // Keyboard navigation on table grid (strictly when table has focus)
  const handleKeyDown = (e: KeyboardEvent<HTMLTableElement>) => {
    if (e.target !== e.currentTarget) return;
    if (rows.length === 0) return;

    if (e.key === "ArrowDown") {
      e.preventDefault();
      setActiveIndex((prev) => (prev < rows.length - 1 ? prev + 1 : prev));
    } else if (e.key === "ArrowUp") {
      e.preventDefault();
      setActiveIndex((prev) => (prev > 0 ? prev - 1 : 0));
    } else if (
      e.key === "Enter" &&
      activeIndex >= 0 &&
      activeIndex < rows.length
    ) {
      e.preventDefault();
      onRowClick?.(rows[activeIndex].original);
    } else if (e.key === " " && activeIndex >= 0 && activeIndex < rows.length) {
      e.preventDefault();
      rows[activeIndex].toggleSelected();
    }
  };

  const activeRowId =
    activeIndex >= 0 && activeIndex < rows.length
      ? `table-row-${rows[activeIndex].id}`
      : undefined;

  const densityPadding =
    density === "compact" ? "py-2.5 px-4 text-xs" : "py-3.5 px-4 text-sm";

  const contextValue = {
    table,
    density,
    setDensity,
    searchValue,
    onSearchChange,
  };

  return (
    <DataTableProvider value={contextValue}>
      <div className={cn("space-y-2 relative", className)}>
        {toolbar ?? <DataTableToolbar />}

        {/* Table Container */}
        <div
          role="region"
          aria-label={t("ariaLabel")}
          className="rounded-[var(--radius-card)] border border-border bg-card overflow-hidden relative min-h-[240px]"
        >
          <TableProgressBar active={Boolean(isFetching && !loading)} />
          <div className="overflow-x-auto max-h-[650px] relative">
            <table
              ref={tableRef}
              role="grid"
              aria-label={t("ariaLabel")}
              aria-activedescendant={activeRowId}
              tabIndex={0}
              onKeyDown={handleKeyDown}
              className="w-full text-left border-collapse focus-visible:outline-2 focus-visible:outline-ring"
            >
              <thead className="border-b border-border bg-muted/50 text-xs font-semibold text-muted-foreground sticky top-0 z-20 backdrop-blur-md">
                {table.getHeaderGroups().map((headerGroup) => (
                  <tr key={headerGroup.id}>
                    {headerGroup.headers.map((header) => {
                      const canSort = header.column.getCanSort();
                      const sorted = header.column.getIsSorted();
                      const ariaSort =
                        sorted === "asc"
                          ? "ascending"
                          : sorted === "desc"
                            ? "descending"
                            : canSort
                              ? "none"
                              : undefined;

                      return (
                        <th
                          key={header.id}
                          aria-sort={ariaSort}
                          className={cn(
                            densityPadding,
                            "font-medium tracking-wide select-none border-b border-border",
                          )}
                        >
                          {header.isPlaceholder ? null : canSort ? (
                            <button
                              type="button"
                              onClick={header.column.getToggleSortingHandler()}
                              className="group/sort -ml-1 inline-flex items-center gap-1.5 rounded-[var(--radius-control)] px-1 py-0.5 font-medium text-inherit hover:bg-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring cursor-pointer transition-colors"
                            >
                              <span>
                                {flexRender(
                                  header.column.columnDef.header,
                                  header.getContext(),
                                )}
                              </span>
                              {sorted === "asc" ? (
                                <ArrowUp className="h-3 w-3 text-foreground" />
                              ) : sorted === "desc" ? (
                                <ArrowDown className="h-3 w-3 text-foreground" />
                              ) : (
                                <ArrowUpDown className="h-3 w-3 text-muted-foreground/60 group-hover/sort:text-foreground transition-colors" />
                              )}
                            </button>
                          ) : (
                            flexRender(
                              header.column.columnDef.header,
                              header.getContext(),
                            )
                          )}
                        </th>
                      );
                    })}
                  </tr>
                ))}
              </thead>

              <tbody
                className={cn(
                  "divide-y divide-border bg-card transition-opacity duration-150",
                  isFetching && !loading && "opacity-60",
                )}
              >
                {loading ? (
                  Array.from({ length: 6 }).map((_, i) => (
                    <tr key={i}>
                      {columns.map((_, colIdx) => (
                        <td key={colIdx} className={densityPadding}>
                          <Skeleton className="h-4 w-full" />
                        </td>
                      ))}
                    </tr>
                  ))
                ) : rows.length > 0 ? (
                  rows.map((row, idx) => {
                    const isActive = idx === activeIndex;
                    const isSelected = row.getIsSelected();

                    return (
                      <tr
                        key={row.id}
                        id={`table-row-${row.id}`}
                        ref={(el) => {
                          rowElementsRef.current[idx] = el;
                        }}
                        data-index={idx}
                        aria-selected={isSelected}
                        onClick={(e) => {
                          const target = e.target as HTMLElement;
                          if (
                            target.closest(
                              "input, button, select, a, [role='checkbox'], [role='button']",
                            )
                          ) {
                            return;
                          }
                          onRowClick?.(row.original);
                        }}
                        className={cn(
                          "transition-colors group",
                          onRowClick && "cursor-pointer",
                          isActive && "ring-2 ring-ring ring-inset bg-muted/60",
                          isSelected
                            ? "bg-primary/5 hover:bg-primary/10"
                            : "hover:bg-muted/40",
                        )}
                      >
                        {row.getVisibleCells().map((cell) => (
                          <td key={cell.id} className={densityPadding}>
                            {flexRender(
                              cell.column.columnDef.cell,
                              cell.getContext(),
                            )}
                          </td>
                        ))}
                      </tr>
                    );
                  })
                ) : (
                  <tr>
                    <td colSpan={columns.length} className="p-8 text-center">
                      <EmptyState
                        title={emptyTitle ?? t("empty.title")}
                        description={emptyDescription ?? t("empty.description")}
                        action={emptyAction}
                      />
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>

        <DataTablePagination paginationSlot={paginationSlot} />

        {/* Floating Bulk Actions Bar */}
        {selectedRows.length > 0 && bulkActions && (
          <div className="sticky bottom-4 z-40 mx-auto flex w-fit items-center gap-3 rounded-[var(--radius-card)] border border-border bg-card/95 px-4 py-2.5 shadow-floating backdrop-blur-md">
            <span
              aria-live="polite"
              className="text-xs font-medium text-foreground"
            >
              {t("selection.selected", {
                count: selectedRows.length,
                total: totalFilteredCount,
              })}
            </span>
            <div className="h-4 w-px bg-border" />
            {bulkActions(selectedRows, () => table.resetRowSelection())}
          </div>
        )}
      </div>
    </DataTableProvider>
  );
}

DataTable.Toolbar = DataTableToolbar;
DataTable.DensityToggle = DataTableDensityToggle;
DataTable.ColumnsMenu = DataTableColumnsMenu;
DataTable.Pagination = DataTablePagination;
