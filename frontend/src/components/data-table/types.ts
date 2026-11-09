import type {
  ColumnDef,
  OnChangeFn,
  PaginationState,
  SortingState,
  Table,
} from "@tanstack/react-table";
import type { ReactNode } from "react";

export type TableDensity = "compact" | "comfortable";

export interface DataTableProps<TData> {
  columns: ColumnDef<TData, unknown>[];
  data: TData[];
  getRowId: (row: TData) => string;
  loading?: boolean;
  isFetching?: boolean;
  emptyTitle?: string;
  emptyDescription?: string;
  emptyAction?: ReactNode;
  onRowClick?: (row: TData) => void;
  bulkActions?: (
    selectedRows: TData[],
    clearSelection: () => void,
  ) => ReactNode;
  initialPageSize?: number;
  className?: string;

  // Controlled TanStack states
  sorting?: SortingState;
  onSortingChange?: OnChangeFn<SortingState>;
  pagination?: PaginationState;
  onPaginationChange?: OnChangeFn<PaginationState>;
  globalFilter?: string;
  onGlobalFilterChange?: OnChangeFn<string>;

  // Server mode
  manualPagination?: boolean;
  manualSorting?: boolean;
  pageCount?: number;
  paginationSlot?: ReactNode;

  // Custom toolbar composition
  toolbar?: ReactNode;
}

export interface DataTableContextValue<TData> {
  table: Table<TData>;
  density: TableDensity;
  setDensity: (d: TableDensity) => void;
  searchValue: string;
  onSearchChange: (value: string) => void;
}
