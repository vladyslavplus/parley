"use client";

import type { ColumnDef } from "@tanstack/react-table";
import { ArrowUpRight, Layers, ListTodo } from "lucide-react";
import { useTranslations } from "next-intl";
import { parseAsString, useQueryState } from "nuqs";
import { useState } from "react";

import { DataTable, useKeysetPaging } from "@/components/data-table";
import {
  FilterChips,
  FiltersButton,
} from "@/components/filters/filters-button";
import { useStaticFilter } from "@/components/filters/use-static-filter";
import { Alert } from "@/components/ui/alert";
import { PERMISSIONS } from "@/constants/permissions";
import { contactRecordRoute } from "@/constants/routes";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { toNumber } from "@/features/campaigns/types";
import { TaskModal } from "@/features/tasks/components/task-modal";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import { useContacts } from "@/features/crm/hooks/use-contacts";
import { useCan } from "@/features/session/components/can";
import { ModeIndicator } from "@/features/insights/components/mode-indicator";
import { NewOrganizationEmpty } from "@/features/insights/components/new-organization-empty";
import {
  actionLabelKey,
  reasonLabelKey,
  SEGMENT_CODES,
  segmentLabelKey,
} from "@/features/insights/lib/codes";
import {
  useRfmSegments,
  usePriority,
} from "@/features/insights/hooks/use-insights";
import type { PriorityItem } from "@/features/insights/types";
import { Link } from "@/i18n/navigation";
import { cn } from "@/lib/utils";

const ICON_BUTTON =
  "inline-flex h-6 w-6 items-center justify-center rounded-md border border-border bg-background text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring";

// The priority list: who to contact today, ranked by the server for this user.
// The server applies the scope; the list shows names, reasons and next steps.
export function PriorityPanel() {
  const t = useTranslations("insights");
  const errorText = useErrorText();
  const [segment, setSegment] = useQueryState(
    "segment",
    parseAsString.withDefault(""),
  );
  const paging = useKeysetPaging();
  const counts = useRfmSegments(true);
  // Segment counts stay zero until the first RFM calculation, so whether any
  // contact exists decides between a new organization and a waiting list.
  const canReadContacts = useCan(PERMISSIONS.contactsRead);
  const anyContact = useContacts({ limit: 1 }, { enabled: canReadContacts });
  const [taskFor, setTaskFor] = useState<PriorityItem | null>(null);

  const query = usePriority(
    { segment },
    { after: paging.after, limit: paging.pageSize },
    true,
  );

  function segmentText(code: string): string | null {
    const key = segmentLabelKey(code);
    return key ? t(key) : null;
  }

  function countOf(code: string): number | null {
    const row = counts.data?.find((item) => item.segment === code);
    return row ? toNumber(row.count) : null;
  }

  const segmentFilter = useStaticFilter({
    key: "segment",
    label: t("filters.segment"),
    icon: Layers,
    value: segment,
    onChange: (value) => {
      void setSegment(value);
      paging.resetPaging();
    },
    options: SEGMENT_CODES.map((code) => {
      const name = segmentText(code) ?? code;
      const count = countOf(code);
      return {
        id: code,
        label: count === null ? name : `${name} · ${count}`,
      };
    }),
    searchPlaceholder: t("filters.searchSegments"),
    emptyLabel: t("filters.noSegments"),
  });

  const rows = query.data?.items ?? [];
  const totalContacts = counts.data
    ? counts.data.reduce((sum, row) => sum + toNumber(row.count), 0)
    : null;
  const hasContacts = anyContact.data ? anyContact.data.items.length > 0 : null;
  const awaitingCalculation =
    !segment &&
    rows.length === 0 &&
    totalContacts === 0 &&
    hasContacts !== false;
  const newOrganization =
    !segment &&
    rows.length === 0 &&
    totalContacts === 0 &&
    hasContacts === false;

  const columns: ColumnDef<PriorityItem, unknown>[] = [
    {
      id: "contact",
      header: t("home.priority.columns.contact"),
      enableSorting: false,
      cell: ({ row }) => (
        <div className="min-w-0">
          <Link
            href={contactRecordRoute(row.original.contactId)}
            className="rounded-sm text-xs font-medium text-foreground underline-offset-2 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
          >
            {row.original.displayName}
          </Link>
          {row.original.companyName ? (
            <p className="truncate text-[11px] text-muted-foreground">
              {row.original.companyName}
            </p>
          ) : null}
        </div>
      ),
    },
    {
      id: "reasons",
      header: t("home.priority.columns.reasons"),
      enableSorting: false,
      cell: ({ row }) => (
        <ul
          className="flex flex-wrap gap-1"
          aria-label={t("home.priority.columns.reasons")}
        >
          {row.original.reasonCodes.map((code) => {
            const key = reasonLabelKey(code);
            if (!key) return null;
            return (
              <li
                key={code}
                className="rounded-full border border-border bg-background px-2 py-0.5 text-[11px] font-medium text-foreground"
              >
                {t(key)}
              </li>
            );
          })}
        </ul>
      ),
    },
    {
      id: "segment",
      header: t("home.priority.columns.segment"),
      enableSorting: false,
      cell: ({ row }) => {
        const text = segmentText(row.original.segment);
        return text ? (
          <span className="text-xs text-foreground">{text}</span>
        ) : (
          <span className="text-muted-foreground/40">—</span>
        );
      },
    },
    {
      id: "next",
      header: t("home.priority.columns.next"),
      enableSorting: false,
      cell: ({ row }) => {
        const actionKey = actionLabelKey(row.original.suggestedAction);
        const name = row.original.displayName;
        return (
          <div className="flex items-center justify-between gap-2">
            <span className="text-xs font-medium text-foreground">
              {actionKey ? t(actionKey) : ""}
            </span>
            <div className="flex items-center gap-1">
              <Link
                href={contactRecordRoute(row.original.contactId)}
                aria-label={`${t("home.priority.openContact")}: ${name}`}
                title={t("home.priority.openContact")}
                className={ICON_BUTTON}
              >
                <ArrowUpRight className="h-3.5 w-3.5" aria-hidden="true" />
              </Link>
              <button
                type="button"
                aria-label={`${t("home.priority.createTask")}: ${name}`}
                title={t("home.priority.createTask")}
                className={cn(ICON_BUTTON, "cursor-pointer")}
                onClick={() => setTaskFor(row.original)}
              >
                <ListTodo className="h-3.5 w-3.5" aria-hidden="true" />
              </button>
            </div>
          </div>
        );
      },
    },
  ];

  if (query.isError && !query.data) {
    return <Alert tone="error">{errorText(query.error)}</Alert>;
  }

  return (
    <section aria-labelledby="priority-title" className="space-y-4">
      <div>
        <h2
          id="priority-title"
          className="text-base font-semibold text-foreground"
        >
          {t("home.priority.title")}
        </h2>
        <p className="mt-0.5 text-xs text-muted-foreground">
          {t("home.priority.subtitle")}
        </p>
      </div>

      <ModeIndicator />

      {newOrganization ? (
        <NewOrganizationEmpty />
      ) : (
        <>
          <p className="text-xs text-muted-foreground">{t("filters.help")}</p>
          <DataTable
            data={rows}
            columns={columns}
            getRowId={(row) => row.contactId}
            loading={query.isPending && !query.data}
            isFetching={query.isFetching}
            manualPagination
            emptyTitle={
              awaitingCalculation
                ? t("home.priority.preparingTitle")
                : t("home.priority.empty")
            }
            emptyDescription={
              awaitingCalculation
                ? t("home.priority.preparingDescription")
                : undefined
            }
            toolbar={
              <div className="flex flex-wrap items-center gap-2">
                <FiltersButton dimensions={[segmentFilter]} />
                <FilterChips dimensions={[segmentFilter]} />
              </div>
            }
            paginationSlot={
              <KeysetPagination
                hasPreviousPage={paging.hasPreviousPage}
                hasNextPage={Boolean(query.data?.nextCursor)}
                onPreviousPage={paging.handlePreviousPage}
                onNextPage={() =>
                  paging.handleNextPage(query.data?.nextCursor ?? null)
                }
                pageSize={paging.pageSize}
                onPageSizeChange={paging.setPageSize}
                itemCount={rows.length}
              />
            }
          />
        </>
      )}

      {taskFor ? (
        <TaskModal
          open
          onOpenChange={(open) => {
            if (!open) setTaskFor(null);
          }}
          initialSubject={{
            contactId: taskFor.contactId,
            contactName: taskFor.displayName,
          }}
        />
      ) : null}
    </section>
  );
}
