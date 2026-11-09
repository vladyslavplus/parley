"use client";

import type { ColumnDef } from "@tanstack/react-table";
import { useFormatter, useTranslations } from "next-intl";
import { parseAsString, useQueryState } from "nuqs";

import {
  DataTable,
  DataTableSearchFilterBar,
  useServerTableState,
} from "@/components/data-table";
import { Alert } from "@/components/ui/alert";
import { Input, Select } from "@/components/ui/input";
import {
  AUDIT_ACTIONS,
  type AuditActionCode,
  auditActionMessageKey,
} from "@/constants/audit-actions";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import { OwnerPicker } from "@/features/crm/components/owner-picker";
import { useAuditEventsPage, useMe } from "@/lib/api/hooks/use-api";
import type { Schemas } from "@/lib/api/schemas";
import {
  displayTimeZone,
  parseUtc,
  toZonedAuditDateRangeUtc,
} from "@/lib/time-zones";

function isKnownAuditAction(code: string): code is AuditActionCode {
  return (AUDIT_ACTIONS as readonly string[]).includes(code);
}

export function AuditPanel() {
  const t = useTranslations("settings.audit");
  const format = useFormatter();

  const [action, setAction] = useQueryState(
    "action",
    parseAsString.withDefault(""),
  );
  const [actorUserId, setActorUserId] = useQueryState(
    "actorUserId",
    parseAsString.withDefault(""),
  );
  const [from, setFrom] = useQueryState("from", parseAsString.withDefault(""));
  const [to, setTo] = useQueryState("to", parseAsString.withDefault(""));

  const tableState = useServerTableState({
    defaultSort: "occurredAt",
    defaultOrder: "desc",
    allowedSorts: ["occurredAt"] as const,
  });

  const { data: me } = useMe();
  const timeZone = displayTimeZone(me);
  const dateRange = toZonedAuditDateRangeUtc(from, to, timeZone);

  const events = useAuditEventsPage(
    {
      after: tableState.after,
      limit: tableState.pageSize,
      q: tableState.q || undefined,
      action: action || undefined,
      actorUserId: actorUserId || undefined,
      from: dateRange.from,
      to: dateRange.to,
      sort: tableState.sort,
      order: tableState.order,
    },
    !dateRange.invalidRange,
  );

  function formatActionLabel(code: string): string {
    if (isKnownAuditAction(code)) {
      return t(
        `actions.${auditActionMessageKey(code)}` as Parameters<typeof t>[0],
      );
    }
    return code;
  }

  if (events.isError && !events.data) {
    return <Alert>{t("loadFailed")}</Alert>;
  }

  const rows = dateRange.invalidRange ? [] : (events.data?.items ?? []);

  const columns: ColumnDef<Schemas["AuditEventView"], unknown>[] = [
    {
      id: "occurredAt",
      accessorKey: "occurredAt",
      header: t("columns.time"),
      enableSorting: true,
      cell: ({ row }) => (
        <span className="text-xs text-foreground whitespace-nowrap">
          {format.dateTime(parseUtc(row.original.occurredAt), {
            dateStyle: "medium",
            timeStyle: "medium",
            timeZone,
          })}
        </span>
      ),
    },
    {
      id: "action",
      accessorKey: "action",
      header: t("columns.action"),
      enableSorting: false,
      cell: ({ row }) => {
        const rawCode = row.original.action;
        const localized = formatActionLabel(rawCode);
        return (
          <div className="flex flex-col gap-0.5">
            <span className="text-xs font-medium text-foreground">
              {localized}
            </span>
            <span className="inline-flex w-fit items-center rounded px-1.5 py-0.5 font-mono text-[11px] bg-muted text-muted-foreground">
              {rawCode}
            </span>
          </div>
        );
      },
    },
    {
      id: "target",
      header: t("columns.target"),
      enableSorting: false,
      cell: ({ row }) => {
        const targetType = row.original.targetType;
        const targetId = row.original.targetId;
        if (!targetType && !targetId) {
          return <span className="text-muted-foreground/40">—</span>;
        }
        return (
          <div className="flex flex-col">
            {targetType ? (
              <span className="text-xs text-foreground">{targetType}</span>
            ) : null}
            {targetId ? (
              <span className="font-mono text-[11px] text-muted-foreground">
                {targetId}
              </span>
            ) : null}
          </div>
        );
      },
    },
    {
      id: "actor",
      accessorKey: "actorUserId",
      header: t("columns.actor"),
      enableSorting: false,
      cell: ({ row }) =>
        row.original.actorUserId ? (
          <span className="font-mono text-[11px] text-muted-foreground">
            {row.original.actorUserId}
          </span>
        ) : (
          <span className="text-muted-foreground/40">—</span>
        ),
    },
  ];

  const hasActiveFilters = Boolean(
    tableState.q || tableState.after || action || actorUserId || from || to,
  );

  return (
    <div className="space-y-4">
      <p className="text-xs text-muted-foreground">{t("subtitle")}</p>

      {dateRange.invalidRange ? (
        <Alert tone="error">{t("invalidDateRange")}</Alert>
      ) : null}

      <DataTable
        data={rows}
        columns={columns}
        getRowId={(row) => row.id}
        loading={!dateRange.invalidRange && events.isPending && !events.data}
        isFetching={events.isFetching}
        manualPagination
        manualSorting
        sorting={tableState.sorting}
        onSortingChange={tableState.onSortingChange}
        emptyTitle={t("empty")}
        toolbar={
          <DataTableSearchFilterBar
            searchInput={tableState.searchInput}
            onSearchInputChange={tableState.setSearchInput}
            onClearSearch={tableState.clearSearch}
            searchPlaceholder={t("searchPlaceholder")}
            hasActiveFilters={hasActiveFilters}
            onResetFilters={() =>
              tableState.resetAll(() => {
                void setAction("");
                void setActorUserId("");
                void setFrom("");
                void setTo("");
              })
            }
          >
            <Select
              aria-label={t("filters.action")}
              value={action}
              onChange={(e) => {
                void setAction(e.target.value);
                tableState.resetPaging();
              }}
              className="h-8 w-auto min-w-[180px] text-xs bg-background py-1"
            >
              <option value="">{t("filters.allActions")}</option>
              {AUDIT_ACTIONS.map((code) => (
                <option key={code} value={code}>
                  {formatActionLabel(code)} ({code})
                </option>
              ))}
            </Select>

            <OwnerPicker
              variant="control"
              className="w-auto min-w-[180px] max-w-[260px]"
              clearLabel={t("filters.allActors")}
              placeholder={t("filters.allActors")}
              ariaLabel={t("filters.actor")}
              value={actorUserId || null}
              onChange={(nextActorId) => {
                void setActorUserId(nextActorId ?? "");
                tableState.resetPaging();
              }}
            />

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
                className="h-8 w-auto text-xs bg-background px-2"
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
                className="h-8 w-auto text-xs bg-background px-2"
              />
            </div>
          </DataTableSearchFilterBar>
        }
        paginationSlot={
          <KeysetPagination
            hasPreviousPage={tableState.hasPreviousPage}
            hasNextPage={Boolean(events.data?.nextCursor)}
            onPreviousPage={tableState.handlePreviousPage}
            onNextPage={() =>
              tableState.handleNextPage(events.data?.nextCursor ?? null)
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
