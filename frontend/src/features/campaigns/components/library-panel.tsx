"use client";

import type { ColumnDef } from "@tanstack/react-table";
import { Archive, Pencil, Plus } from "lucide-react";
import type { ReactNode } from "react";

import { DataTable } from "@/components/data-table/data-table";
import { DataTableSearchFilterBar } from "@/components/data-table/data-table-search-filter-bar";
import type { useServerTableState } from "@/components/data-table/use-server-table-state";
import { Button } from "@/components/ui/button";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";

export interface LibraryRow {
  id: string;
  name: string;
}

interface Props<TRow extends LibraryRow> {
  rows: TRow[];
  nextCursor: string | null;
  isLoading: boolean;
  isFetching: boolean;
  isError: boolean;
  onRetry: () => void;
  tableState: ReturnType<typeof useServerTableState>;
  searchPlaceholder: string;
  emptyTitle: string;
  emptyDescription: string;
  createLabel: string;
  canManage: boolean;
  onCreate: () => void;
  onEdit: (row: TRow) => void;
  onArchive: (row: TRow) => void;
  editLabel: string;
  archiveLabel: string;
  loadError: string;
  retryLabel: string;
  nameLabel: string;
  extraColumns?: ColumnDef<TRow, unknown>[];
}

// List page shared by the template and segment libraries: search, keyset
// paging in the URL, edit/archive icon actions and an empty state with action.
export function LibraryPanel<TRow extends LibraryRow>({
  rows,
  nextCursor,
  isLoading,
  isFetching,
  isError,
  onRetry,
  tableState,
  searchPlaceholder,
  emptyTitle,
  emptyDescription,
  createLabel,
  canManage,
  onCreate,
  onEdit,
  onArchive,
  editLabel,
  archiveLabel,
  loadError,
  retryLabel,
  nameLabel,
  extraColumns = [],
}: Props<TRow>) {
  const columns: ColumnDef<TRow, unknown>[] = [
    {
      id: "name",
      header: nameLabel,
      cell: ({ row }) =>
        canManage ? (
          <button
            type="button"
            onClick={() => onEdit(row.original)}
            className="cursor-pointer text-left text-xs font-medium text-foreground hover:underline focus-visible:outline-2 focus-visible:outline-ring"
          >
            {row.original.name}
          </button>
        ) : (
          <span className="text-xs font-medium text-foreground">
            {row.original.name}
          </span>
        ),
    },
    ...extraColumns,
    {
      id: "actions",
      header: "",
      size: 80,
      cell: ({ row }) =>
        canManage ? (
          <div className="flex items-center justify-end gap-1">
            <button
              type="button"
              onClick={() => onEdit(row.original)}
              aria-label={`${editLabel}: ${row.original.name}`}
              title={editLabel}
              className="flex h-6 w-6 cursor-pointer items-center justify-center rounded-xs text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
            >
              <Pencil className="h-3.5 w-3.5" />
            </button>
            <button
              type="button"
              onClick={() => onArchive(row.original)}
              aria-label={`${archiveLabel}: ${row.original.name}`}
              title={archiveLabel}
              className="flex h-6 w-6 cursor-pointer items-center justify-center rounded-xs text-muted-foreground hover:bg-destructive/10 hover:text-destructive focus-visible:outline-2 focus-visible:outline-ring"
            >
              <Archive className="h-3.5 w-3.5" />
            </button>
          </div>
        ) : null,
    },
  ];

  const createButton: ReactNode = canManage ? (
    <Button
      type="button"
      variant="secondary"
      onClick={onCreate}
      className="gap-1.5 px-3 text-xs"
    >
      <Plus className="h-4 w-4" />
      <span>{createLabel}</span>
    </Button>
  ) : null;

  return (
    <DataTable
      data={rows}
      columns={columns}
      getRowId={(row) => row.id}
      loading={isLoading}
      isFetching={isFetching}
      manualPagination
      manualSorting
      emptyTitle={isError ? loadError : emptyTitle}
      emptyDescription={isError ? undefined : emptyDescription}
      emptyAction={
        isError ? (
          <Button
            type="button"
            variant="secondary"
            onClick={onRetry}
            className="px-3 text-xs"
          >
            {retryLabel}
          </Button>
        ) : (
          createButton
        )
      }
      toolbar={
        <DataTableSearchFilterBar
          searchInput={tableState.searchInput}
          onSearchInputChange={tableState.setSearchInput}
          onClearSearch={tableState.clearSearch}
          searchPlaceholder={searchPlaceholder}
          actions={createButton}
        />
      }
      paginationSlot={
        <KeysetPagination
          hasPreviousPage={tableState.hasPreviousPage}
          hasNextPage={Boolean(nextCursor)}
          onPreviousPage={tableState.handlePreviousPage}
          onNextPage={() => tableState.handleNextPage(nextCursor)}
          pageSize={tableState.pageSize}
          onPageSizeChange={tableState.setPageSize}
          itemCount={rows.length}
        />
      }
    />
  );
}
