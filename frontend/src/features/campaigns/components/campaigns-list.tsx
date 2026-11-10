"use client";

import type { ColumnDef } from "@tanstack/react-table";
import { Plus } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { parseAsString, useQueryState } from "nuqs";

import { DataTable } from "@/components/data-table/data-table";
import { DataTableSearchFilterBar } from "@/components/data-table/data-table-search-filter-bar";
import { useServerTableState } from "@/components/data-table/use-server-table-state";
import { Button, buttonVariants } from "@/components/ui/button";
import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES, campaignRoute } from "@/constants/routes";
import { CampaignStatusBadge } from "@/features/campaigns/components/campaign-status-badge";
import { useCampaigns } from "@/features/campaigns/hooks/use-campaign-api";
import { useStatusDimension } from "@/features/campaigns/hooks/use-status-dimension";
import {
  CAMPAIGN_SOURCES,
  toNumber,
  type CampaignSummary,
} from "@/features/campaigns/types";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import { useCan } from "@/features/session/components/can";
import { Link, useRouter } from "@/i18n/navigation";
import { formatDate } from "@/lib/formatters";

const NEWEST_FIRST = { sort: "created", order: "desc" } as const;

export function CampaignsList() {
  const t = useTranslations("campaigns.list");
  const tReview = useTranslations("campaigns.builder.review");
  const locale = useLocale();
  const router = useRouter();
  const canManage = useCan(PERMISSIONS.campaignsManage);

  const tableState = useServerTableState({ defaultSort: NEWEST_FIRST.sort });
  const [status, setStatus] = useQueryState(
    "status",
    parseAsString.withDefault(""),
  );
  const [, setAfter] = useQueryState("after", parseAsString.withDefault(""));
  const statusDimension = useStatusDimension({
    value: status,
    onChange: (next) => {
      void setStatus(next || null);
      void setAfter("");
    },
  });

  const query = useCampaigns({
    q: tableState.q || undefined,
    status: status || undefined,
    ...NEWEST_FIRST,
    after: tableState.after || null,
    limit: tableState.pageSize,
  });
  const rows = query.data?.items ?? [];

  const columns: ColumnDef<CampaignSummary, unknown>[] = [
    {
      id: "name",
      header: t("columns.name"),
      cell: ({ row }) => (
        <div className="flex min-w-0 items-center gap-2">
          <Link
            href={campaignRoute(row.original.id)}
            onClick={(event) => event.stopPropagation()}
            className="truncate text-xs font-medium text-foreground hover:underline focus-visible:outline-2 focus-visible:outline-ring"
          >
            {row.original.name}
          </Link>
          {row.original.source === CAMPAIGN_SOURCES.demoSeed ? (
            <span className="inline-flex h-5 shrink-0 items-center rounded-full bg-muted px-2 text-[11px] font-medium text-foreground">
              {t("demoBadge")}
            </span>
          ) : null}
        </div>
      ),
    },
    {
      id: "status",
      header: t("columns.status"),
      size: 140,
      cell: ({ row }) => <CampaignStatusBadge status={row.original.status} />,
    },
    {
      id: "holdout",
      header: t("columns.holdout"),
      size: 110,
      cell: ({ row }) => (
        <span className="text-xs tabular-nums text-muted-foreground">
          {toNumber(row.original.holdoutShareBps) / 100}%
        </span>
      ),
    },
    {
      id: "targeting",
      header: t("columns.targeting"),
      size: 180,
      cell: ({ row }) => {
        const mode = row.original.targetingMode;
        return (
          <span className="text-xs text-muted-foreground">
            {mode === "top_uplift"
              ? tReview("topUplift")
              : mode === "top_rfm"
                ? tReview("topRfm")
                : tReview("everyone")}
          </span>
        );
      },
    },
    {
      id: "started",
      header: t("columns.started"),
      size: 150,
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground">
          {row.original.startedAt
            ? formatDate(row.original.startedAt, locale)
            : "—"}
        </span>
      ),
    },
  ];

  const createButton = canManage ? (
    <Link
      href={ROUTES.campaignNew}
      className={buttonVariants({
        variant: "secondary",
        className: "gap-1.5 px-3 text-xs",
      })}
    >
      <Plus className="h-4 w-4" />
      <span>{t("create")}</span>
    </Link>
  ) : null;

  const filtered = Boolean(tableState.q || status);

  return (
    <DataTable
      data={rows}
      columns={columns}
      getRowId={(row) => row.id}
      loading={query.isPending && !query.data}
      isFetching={query.isFetching}
      manualPagination
      manualSorting
      onRowClick={(row) => router.push(campaignRoute(row.id))}
      emptyTitle={
        query.isError
          ? t("loadError")
          : filtered
            ? t("emptyFiltered.title")
            : t("empty.title")
      }
      emptyDescription={
        query.isError
          ? undefined
          : filtered
            ? t("emptyFiltered.description")
            : t("empty.description")
      }
      emptyAction={
        query.isError ? (
          <Button
            type="button"
            variant="secondary"
            onClick={() => void query.refetch()}
            className="px-3 text-xs"
          >
            {t("retry")}
          </Button>
        ) : filtered ? null : (
          createButton
        )
      }
      toolbar={
        <DataTableSearchFilterBar
          searchInput={tableState.searchInput}
          onSearchInputChange={tableState.setSearchInput}
          onClearSearch={tableState.clearSearch}
          searchPlaceholder={t("searchPlaceholder")}
          filters={[statusDimension]}
        />
      }
      paginationSlot={
        <KeysetPagination
          hasPreviousPage={tableState.hasPreviousPage}
          hasNextPage={Boolean(query.data?.nextCursor)}
          onPreviousPage={tableState.handlePreviousPage}
          onNextPage={() =>
            tableState.handleNextPage(query.data?.nextCursor ?? null)
          }
          pageSize={tableState.pageSize}
          onPageSizeChange={tableState.setPageSize}
          itemCount={rows.length}
        />
      }
    />
  );
}
