"use client";

import type { ColumnDef } from "@tanstack/react-table";
import { useFormatter, useLocale, useTranslations } from "next-intl";
import { parseAsString, useQueryState } from "nuqs";

import {
  DataTable,
  DataTableSearchFilterBar,
  useServerTableState,
} from "@/components/data-table";
import { Alert } from "@/components/ui/alert";
import { Input } from "@/components/ui/input";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import { useOrders } from "@/features/orders/hooks/use-orders";
import { ORDER_SORTS, type Order } from "@/features/orders/types";
import { useMe } from "@/lib/api/hooks/use-api";
import { formatCurrency } from "@/lib/formatters";
import {
  displayTimeZone,
  parseUtc,
  toZonedAuditDateRangeUtc,
} from "@/lib/time-zones";

interface Props {
  contactId: string;
}

export function OrdersPanel({ contactId }: Props) {
  const t = useTranslations("crm.orders");
  const locale = useLocale();
  const format = useFormatter();

  const [from, setFrom] = useQueryState(
    "orderedFrom",
    parseAsString.withDefault(""),
  );
  const [to, setTo] = useQueryState("orderedTo", parseAsString.withDefault(""));

  const tableState = useServerTableState({
    defaultSort: "ordered",
    defaultOrder: "desc",
    allowedSorts: ORDER_SORTS,
  });

  const { data: me } = useMe();
  const timeZone = displayTimeZone(me);
  const dateRange = toZonedAuditDateRangeUtc(from, to, timeZone);

  const orders = useOrders(
    {
      contactId,
      q: tableState.q || undefined,
      orderedFrom: dateRange.from,
      orderedTo: dateRange.to,
      sort: tableState.sort,
      order: tableState.order,
      after: tableState.after,
      limit: tableState.pageSize,
    },
    { enabled: !dateRange.invalidRange },
  );

  if (orders.isError && !orders.data) {
    return <Alert>{t("loadFailed")}</Alert>;
  }

  const rows = dateRange.invalidRange ? [] : (orders.data?.items ?? []);

  const columns: ColumnDef<Order, unknown>[] = [
    {
      id: "invoice",
      accessorKey: "externalRef",
      header: t("columns.invoice"),
      enableSorting: false,
      cell: ({ row }) => (
        <div className="flex items-center gap-2">
          <span className="font-mono text-[11px] text-foreground">
            {row.original.externalRef}
          </span>
          {row.original.isCancellation ? (
            <span className="inline-flex items-center rounded px-1.5 py-0.5 text-[11px] font-medium bg-destructive/10 text-destructive">
              {t("cancellation")}
            </span>
          ) : null}
        </div>
      ),
    },
    {
      id: "ordered",
      accessorKey: "orderedAt",
      header: t("columns.ordered"),
      enableSorting: true,
      cell: ({ row }) => (
        <span className="text-xs text-foreground whitespace-nowrap">
          {format.dateTime(parseUtc(row.original.orderedAt), {
            dateStyle: "medium",
            timeZone,
          })}
        </span>
      ),
    },
    {
      id: "lines",
      accessorKey: "lineCount",
      header: t("columns.lines"),
      enableSorting: false,
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground tabular-nums">
          {row.original.lineCount}
        </span>
      ),
    },
    {
      id: "amount",
      accessorKey: "totalAmount",
      header: t("columns.total"),
      enableSorting: true,
      cell: ({ row }) => (
        <span className="text-xs font-medium text-foreground tabular-nums whitespace-nowrap">
          {formatCurrency(
            Number(row.original.totalAmount),
            row.original.currency,
            locale,
          )}
        </span>
      ),
    },
  ];

  const hasActiveFilters = Boolean(
    tableState.q || tableState.after || from || to,
  );

  return (
    <div className="space-y-4">
      <h3 className="text-sm font-semibold text-foreground">{t("title")}</h3>

      {dateRange.invalidRange ? (
        <Alert tone="error">{t("invalidDateRange")}</Alert>
      ) : null}

      <DataTable
        data={rows}
        columns={columns}
        getRowId={(row) => row.id}
        loading={!dateRange.invalidRange && orders.isPending && !orders.data}
        isFetching={orders.isFetching}
        manualPagination
        manualSorting
        sorting={tableState.sorting}
        onSortingChange={tableState.onSortingChange}
        emptyTitle={hasActiveFilters ? t("emptyFiltered") : t("empty")}
        toolbar={
          <DataTableSearchFilterBar
            searchInput={tableState.searchInput}
            onSearchInputChange={tableState.setSearchInput}
            onClearSearch={tableState.clearSearch}
            searchPlaceholder={t("searchPlaceholder")}
            hasActiveFilters={hasActiveFilters}
            onResetFilters={() =>
              tableState.resetAll(() => {
                void setFrom("");
                void setTo("");
              })
            }
          >
            <div className="flex items-center gap-1.5">
              <Input
                type="date"
                aria-label={t("filters.from")}
                value={from}
                max={to || undefined}
                onChange={(e) => {
                  const nextFrom = e.target.value;
                  void setFrom(nextFrom);
                  if (nextFrom && to && nextFrom > to) {
                    void setTo(nextFrom);
                  }
                  tableState.resetPaging();
                }}
                className="w-auto text-xs bg-background px-2"
              />
              <span className="text-xs text-muted-foreground">—</span>
              <Input
                type="date"
                aria-label={t("filters.to")}
                value={to}
                min={from || undefined}
                onChange={(e) => {
                  const nextTo = e.target.value;
                  void setTo(nextTo);
                  if (nextTo && from && nextTo < from) {
                    void setFrom(nextTo);
                  }
                  tableState.resetPaging();
                }}
                className="w-auto text-xs bg-background px-2"
              />
            </div>
          </DataTableSearchFilterBar>
        }
        paginationSlot={
          <KeysetPagination
            hasPreviousPage={tableState.hasPreviousPage}
            hasNextPage={Boolean(orders.data?.nextCursor)}
            onPreviousPage={tableState.handlePreviousPage}
            onNextPage={() =>
              tableState.handleNextPage(orders.data?.nextCursor ?? null)
            }
            pageSize={tableState.pageSize}
            onPageSizeChange={tableState.setPageSize}
            itemCount={rows.length}
          />
        }
      />
    </div>
  );
}
