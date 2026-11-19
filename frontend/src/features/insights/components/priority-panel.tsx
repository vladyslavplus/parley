"use client";

import type { ColumnDef } from "@tanstack/react-table";
import { Layers, Target } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { parseAsString, useQueryState } from "nuqs";
import { useEffect, useState } from "react";

import { DataTable, useKeysetPaging } from "@/components/data-table";
import {
  FilterChips,
  FiltersButton,
} from "@/components/filters/filters-button";
import { useStaticFilter } from "@/components/filters/use-static-filter";
import { Alert } from "@/components/ui/alert";
import { InfoHint } from "@/components/ui/info-hint";
import { PERMISSIONS } from "@/constants/permissions";
import { contactRecordRoute } from "@/constants/routes";
import { useScoringModels } from "@/features/insights/hooks/use-scoring";
import { readSegmentRules } from "@/features/insights/lib/segment-rules";
import { formatThresholdPoints } from "@/features/insights/lib/score-format";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { toNumber } from "@/features/campaigns/types";
import { TaskModal } from "@/features/tasks/components/task-modal";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import { useContacts } from "@/features/crm/hooks/use-contacts";
import { useCan } from "@/features/session/components/can";
import { ModeIndicator } from "@/features/insights/components/mode-indicator";
import { NewOrganizationEmpty } from "@/features/insights/components/new-organization-empty";
import { NextStepCell } from "@/features/insights/components/next-step-cell";
import {
  NextStepHint,
  ReasonsHint,
  SegmentColumnHint,
  UpliftColumnHint,
} from "@/features/insights/components/priority-hints";
import { UpliftCell } from "@/features/insights/components/uplift-cell";
import { ReasonChips } from "@/features/insights/components/reason-chips";
import {
  RfmSegmentBadge,
  UpliftSegmentBadge,
} from "@/features/insights/components/segment-badge";
import {
  SEGMENT_CODES,
  UPLIFT_SEGMENT_CODES,
  segmentLabelKey,
  upliftSegmentLabelKey,
} from "@/features/insights/lib/codes";
import {
  usePriority,
  usePrioritySegments,
} from "@/features/insights/hooks/use-insights";
import { isStaleCursorError } from "@/features/insights/lib/priority-cursor";
import type {
  PriorityItem,
  PrioritySegmentCount,
} from "@/features/insights/types";
import { toApiError } from "@/lib/errors";
import { formatNumber } from "@/lib/formatters";
import { Link } from "@/i18n/navigation";

const POINTS_DIGITS = 1;

// The priority list: who to contact today, ranked by the server for this user.
// The list names the segment the ranking uses, why each row is here and the
// next step; the numbers stay behind the ML-view permission.
export function PriorityPanel() {
  const t = useTranslations("insights");
  const tHints = useTranslations("hints");
  const locale = useLocale();
  const errorText = useErrorText();
  const [segment, setSegment] = useQueryState(
    "segment",
    parseAsString.withDefault(""),
  );
  const [upliftSegment, setUpliftSegment] = useQueryState(
    "upliftSegment",
    parseAsString.withDefault(""),
  );
  const paging = useKeysetPaging();
  const segments = usePrioritySegments(true);
  // Segment counts stay zero until the first RFM calculation, so whether any
  // contact exists decides between a new organization and a waiting list.
  const canReadContacts = useCan(PERMISSIONS.contactsRead);
  const canRunScoring = useCan(PERMISSIONS.insightsMlRun);
  const canViewNumbers = useCan(PERMISSIONS.insightsMlView);
  const anyContact = useContacts({ limit: 1 }, { enabled: canReadContacts });
  const [taskFor, setTaskFor] = useState<PriorityItem | null>(null);
  const models = useScoringModels(canViewNumbers);
  // The thresholds come from the active model's own manifest, never typed here.
  const segmentRules = readSegmentRules(
    models.data?.find((model) => model.kind === "uplift" && model.isActive)
      ?.segmentRules ?? null,
  );
  // The mode the last response reported, so a filter left in the URL from an
  // ML list does not empty an RFM list after ML is switched off.
  const [listMode, setListMode] = useState<string | undefined>(undefined);

  const query = usePriority(
    { segment, upliftSegment: listMode === "rfm" ? "" : upliftSegment },
    { after: paging.after, limit: paging.pageSize },
    true,
  );
  const serverMode = query.data?.mode;
  if (serverMode && serverMode !== listMode) setListMode(serverMode);

  // A stale cursor restarts the list from the first page (see priority-cursor.ts).
  const staleCursor =
    query.isError &&
    isStaleCursorError(paging.after, toApiError(query.error).code);
  const { resetPaging } = paging;
  useEffect(() => {
    if (staleCursor) resetPaging();
  }, [staleCursor, resetPaging]);

  function countOf(
    taxonomy: "rfm" | "uplift",
    code: string,
  ): PrioritySegmentCount | undefined {
    return segments.data?.[taxonomy].find((item) => item.segment === code);
  }

  function countLabel(count: PrioritySegmentCount | undefined): string | null {
    if (!count) return null;
    return t("filters.countOfTotal", {
      onList: formatNumber(toNumber(count.onList), locale),
      total: formatNumber(toNumber(count.total), locale),
    });
  }

  function optionLabel(
    taxonomy: "rfm" | "uplift",
    code: string,
    name: string,
  ): string {
    const counts = countLabel(countOf(taxonomy, code));
    return counts ? `${name} · ${counts}` : name;
  }

  const upliftFilter = useStaticFilter({
    key: "upliftSegment",
    label: t("filters.upliftSegment"),
    icon: Target,
    value: upliftSegment,
    onChange: (value) => {
      void setUpliftSegment(value);
      paging.resetPaging();
    },
    options: UPLIFT_SEGMENT_CODES.map((code) => ({
      id: code,
      label: optionLabel(
        "uplift",
        code,
        t(upliftSegmentLabelKey(code) ?? "filters.upliftSegment"),
      ),
    })),
    searchPlaceholder: t("filters.searchUpliftSegments"),
    emptyLabel: t("filters.noSegments"),
  });

  const rfmFilter = useStaticFilter({
    key: "segment",
    label: t("filters.rfmSegment"),
    icon: Layers,
    value: segment,
    onChange: (value) => {
      void setSegment(value);
      paging.resetPaging();
    },
    options: SEGMENT_CODES.map((code) => ({
      id: code,
      label: optionLabel(
        "rfm",
        code,
        t(segmentLabelKey(code) ?? "filters.segment"),
      ),
    })),
    searchPlaceholder: t("filters.searchSegments"),
    emptyLabel: t("filters.noSegments"),
  });

  const rows = query.data?.items ?? [];
  // A fraction of the uplift scale; the list reports the configured mark.
  const highUpliftPoints = query.data
    ? formatThresholdPoints(
        toNumber(query.data.upliftHighThreshold) * 100,
        locale,
      )
    : null;
  const staleDealDays = query.data ? toNumber(query.data.staleDealDays) : null;
  const totalContacts = segments.data
    ? segments.data.rfm.reduce((sum, row) => sum + toNumber(row.total), 0)
    : null;
  const hasContacts = anyContact.data ? anyContact.data.items.length > 0 : null;
  const awaitingCalculation =
    !segment &&
    !upliftSegment &&
    rows.length === 0 &&
    totalContacts === 0 &&
    hasContacts !== false;
  const newOrganization =
    !segment &&
    !upliftSegment &&
    rows.length === 0 &&
    totalContacts === 0 &&
    hasContacts === false;

  // In ML mode the model's segment is the row label; the RFM segment stays as
  // the muted second line. The counts endpoint knows whether the uplift
  // taxonomy exists for this organization, so it decides the same way the
  // server's mode does.
  const mlMode = query.data?.mode === "ml";
  const dimensions = mlMode ? [upliftFilter, rfmFilter] : [rfmFilter];

  const filtered = Boolean(segment || upliftSegment);
  const filteredCount =
    (upliftSegment ? countOf("uplift", upliftSegment) : undefined) ??
    (segment ? countOf("rfm", segment) : undefined);
  const filteredName = upliftSegment
    ? t(upliftSegmentLabelKey(upliftSegment) ?? "filters.upliftSegment")
    : segment
      ? t(segmentLabelKey(segment) ?? "filters.segment")
      : null;
  const emptyFilteredTitle =
    filtered && rows.length === 0
      ? t("home.priority.emptyFilteredTitle")
      : null;
  const emptyFilteredDescription = filtered
    ? filteredCount && filteredName
      ? t("home.priority.emptyFilteredDescription", {
          segment: filteredName,
          total: formatNumber(toNumber(filteredCount.total), locale),
        })
      : t("home.priority.emptyFilteredDescriptionUnknown")
    : undefined;

  function upliftPoints(value: number | string | null): string | null {
    if (value === null) return null;
    return new Intl.NumberFormat(locale, {
      signDisplay: "exceptZero",
      minimumFractionDigits: POINTS_DIGITS,
      maximumFractionDigits: POINTS_DIGITS,
    }).format(toNumber(value) * 100);
  }

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
      header: () => (
        <span className="inline-flex items-center gap-1">
          {t("home.priority.columns.reasons")}
          <ReasonsHint
            mlMode={mlMode}
            highUpliftPoints={highUpliftPoints}
            staleDealDays={staleDealDays}
          />
        </span>
      ),
      enableSorting: false,
      cell: ({ row }) => <ReasonChips codes={row.original.reasonCodes} />,
    },
    {
      id: "segment",
      size: 190,
      header: () => (
        <span className="inline-flex items-center gap-1">
          {t("home.priority.columns.segment")}
          <SegmentColumnHint mlMode={mlMode} rules={segmentRules} />
        </span>
      ),
      enableSorting: false,
      cell: ({ row }) => (
        <div className="space-y-1">
          {mlMode && row.original.upliftSegment ? (
            <>
              <UpliftSegmentBadge code={row.original.upliftSegment} />
              <div>
                <RfmSegmentBadge code={row.original.segment} withLabel />
              </div>
            </>
          ) : (
            <RfmSegmentBadge code={row.original.segment} />
          )}
        </div>
      ),
    },
  ];

  if (mlMode && canViewNumbers) {
    columns.push({
      id: "upliftValue",
      size: 130,
      header: () => (
        <span className="flex items-center justify-end gap-1">
          {t("home.priority.upliftColumn")}
          <UpliftColumnHint
            rules={segmentRules}
            highUpliftPoints={highUpliftPoints}
          />
        </span>
      ),
      enableSorting: false,
      cell: ({ row }) => {
        const points = upliftPoints(row.original.upliftValue);
        if (!points) {
          return <div className="text-right text-muted-foreground/40">—</div>;
        }
        return (
          <div className="text-right">
            <UpliftCell
              contactId={row.original.contactId}
              displayName={row.original.displayName}
              points={points}
              negative={toNumber(row.original.upliftValue ?? 0) < 0}
            />
          </div>
        );
      },
    });
  }

  columns.push({
    id: "next",
    size: 240,
    header: () => (
      <span className="inline-flex items-center gap-1">
        {t("home.priority.columns.next")}
        <NextStepHint mlMode={mlMode} staleDealDays={staleDealDays} />
      </span>
    ),
    enableSorting: false,
    cell: ({ row }) => (
      <NextStepCell item={row.original} onCreateTask={setTaskFor} />
    ),
  });

  if (query.isError && !query.data && !staleCursor) {
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
          {query.data?.mode === "ml"
            ? t("home.priority.subtitleMl")
            : t("home.priority.subtitle")}
        </p>
      </div>

      <ModeIndicator
        mode={query.data?.mode}
        mlStatus={query.data?.mlStatus}
        modelProvenance={query.data?.modelProvenance}
        canRun={canRunScoring}
      />

      {newOrganization ? (
        <NewOrganizationEmpty />
      ) : (
        <>
          <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <span>{t("filters.help")}</span>
            <InfoHint>{tHints("priorityCounts")}</InfoHint>
          </p>
          <DataTable
            data={rows}
            columns={columns}
            getRowId={(row) => row.contactId}
            loading={query.isPending && !query.data}
            isFetching={query.isFetching}
            manualPagination
            emptyTitle={
              emptyFilteredTitle ??
              (awaitingCalculation
                ? t("home.priority.preparingTitle")
                : t("home.priority.empty"))
            }
            emptyDescription={
              emptyFilteredTitle
                ? emptyFilteredDescription
                : awaitingCalculation
                  ? t("home.priority.preparingDescription")
                  : undefined
            }
            toolbar={
              <div className="flex flex-wrap items-center gap-2">
                <FiltersButton dimensions={dimensions} />
                <FilterChips dimensions={dimensions} />
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
