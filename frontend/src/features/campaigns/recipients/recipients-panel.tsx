"use client";

import type { ColumnDef } from "@tanstack/react-table";
import { CircleCheck, Crosshair, Send, Users } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { parseAsString, useQueryState } from "nuqs";

import {
  DataTable,
  DataTableSearchFilterBar,
  useServerTableState,
} from "@/components/data-table";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { PERMISSIONS } from "@/constants/permissions";
import { contactRecordRoute } from "@/constants/routes";
import { useOrganizationCurrency } from "@/features/deals/hooks/use-organization-currency";
import { useCampaignRecipients } from "@/features/campaigns/hooks/use-campaign-recipients";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { recipientEmail } from "@/features/campaigns/recipients/recipient-email";
import { useStaticFilter } from "@/components/filters/use-static-filter";
import {
  CONVERSION_DELETED,
  CONVERSION_NOT_VISIBLE,
  type CampaignRecipient,
  toNumber,
} from "@/features/campaigns/types";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import { useCan } from "@/features/session/components/can";
import { Link } from "@/i18n/navigation";
import { formatCurrency, formatDateTime } from "@/lib/formatters";

// The recipients of a campaign whose audience is frozen. Rows are read one
// keyset page at a time; filters and the search live in the URL.
export function RecipientsPanel({
  campaignId,
  isDemo,
}: {
  campaignId: string;
  // Seeded from the historic dataset: no email was sent to anyone.
  isDemo: boolean;
}) {
  const t = useTranslations("campaigns.recipients");
  const locale = useLocale();
  const errorText = useErrorText();
  const currency = useOrganizationCurrency();
  const canOpenContacts = useCan(PERMISSIONS.contactsRead);

  const [arm, setArm] = useQueryState("arm", parseAsString.withDefault(""));
  const [targeted, setTargeted] = useQueryState(
    "targeted",
    parseAsString.withDefault(""),
  );
  const [sendStatus, setSendStatus] = useQueryState(
    "sendStatus",
    parseAsString.withDefault(""),
  );
  const [converted, setConverted] = useQueryState(
    "converted",
    parseAsString.withDefault(""),
  );

  const tableState = useServerTableState({
    defaultSort: "id",
    allowedSorts: ["id"] as const,
  });

  const query = useCampaignRecipients(
    campaignId,
    { q: tableState.q, arm, targeted, sendStatus, converted },
    { after: tableState.after, limit: tableState.pageSize },
    true,
  );

  function changeFilter(set: (value: string) => unknown, value: string) {
    void set(value);
    tableState.resetPaging();
  }

  const armFilter = useStaticFilter({
    key: "arm",
    label: t("columns.group"),
    icon: Users,
    value: arm,
    onChange: (value) => changeFilter(setArm, value),
    options: [
      { id: "treatment", label: t("arm.treatment") },
      { id: "holdout", label: t("arm.holdout") },
    ],
    searchPlaceholder: t("filters.search"),
    emptyLabel: t("filters.empty"),
  });

  const targetedFilter = useStaticFilter({
    key: "targeted",
    label: t("columns.targeted"),
    icon: Crosshair,
    value: targeted,
    onChange: (value) => changeFilter(setTargeted, value),
    options: [
      { id: "true", label: t("filters.targetedOnly") },
      { id: "false", label: t("filters.notTargetedOnly") },
    ],
    searchPlaceholder: t("filters.search"),
    emptyLabel: t("filters.empty"),
  });

  const sendStatusFilter = useStaticFilter({
    key: "sendStatus",
    label: t("columns.sendStatus"),
    icon: Send,
    value: sendStatus,
    onChange: (value) => changeFilter(setSendStatus, value),
    options: [
      { id: "claimed", label: t("sendStatus.claimed") },
      { id: "sent", label: t("sendStatus.sent") },
      { id: "retryable_failed", label: t("sendStatus.retryable_failed") },
      { id: "failed", label: t("sendStatus.failed") },
      { id: "skipped_suppressed", label: t("sendStatus.skipped_suppressed") },
      { id: "unknown", label: t("sendStatus.unknown") },
    ],
    searchPlaceholder: t("filters.search"),
    emptyLabel: t("filters.empty"),
  });

  const convertedFilter = useStaticFilter({
    key: "converted",
    label: t("columns.conversion"),
    icon: CircleCheck,
    value: converted,
    onChange: (value) => changeFilter(setConverted, value),
    options: [
      { id: "true", label: t("filters.convertedOnly") },
      { id: "false", label: t("filters.notConvertedOnly") },
    ],
    searchPlaceholder: t("filters.search"),
    emptyLabel: t("filters.empty"),
  });

  if (query.isError && !query.data) {
    return <Alert tone="error">{errorText(query.error)}</Alert>;
  }

  const rows = query.data?.items ?? [];
  // The server scans a capped number of rows per page: an empty page with a
  // cursor means the search continues, not that nothing matches.
  const scanning =
    !query.isPending && rows.length === 0 && Boolean(query.data?.nextCursor);

  function sendStatusLabel(code: string): string {
    switch (code) {
      case "claimed":
        return t("sendStatus.claimed");
      case "sent":
        return t("sendStatus.sent");
      case "retryable_failed":
        return t("sendStatus.retryable_failed");
      case "failed":
        return t("sendStatus.failed");
      case "skipped_suppressed":
        return t("sendStatus.skipped_suppressed");
      case "unknown":
        return t("sendStatus.unknown");
      default:
        return code;
    }
  }

  function conversionLabel(code: string): string {
    switch (code) {
      case "converted":
        return t("conversion.converted");
      case "not_converted":
        return t("conversion.not_converted");
      case "pending":
        return t("conversion.pending");
      case "not_targeted":
        return t("conversion.not_targeted");
      case "not_visible":
        return t("conversion.not_visible");
      case "deleted":
        return t("conversion.deleted");
      default:
        return code;
    }
  }

  const columns: ColumnDef<CampaignRecipient, unknown>[] = [
    {
      id: "contact",
      header: t("columns.contact"),
      enableSorting: false,
      cell: ({ row }) => {
        const name = row.original.contactName;
        if (!row.original.nameVisible) {
          return (
            <span className="text-xs text-muted-foreground">
              {t("contactHidden")}
            </span>
          );
        }
        if (!name) {
          return (
            <span className="text-xs text-muted-foreground">
              {t("contactRemoved")}
            </span>
          );
        }
        // A deleted contact has no record to open: the name is shown as text.
        if (
          !canOpenContacts ||
          row.original.conversion === CONVERSION_DELETED
        ) {
          return (
            <span className="text-xs font-medium text-foreground">{name}</span>
          );
        }
        return (
          <Link
            href={contactRecordRoute(row.original.contactId)}
            className="rounded-sm text-xs font-medium text-foreground underline-offset-2 hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring"
          >
            {name}
          </Link>
        );
      },
    },
    {
      id: "group",
      header: t("columns.group"),
      enableSorting: false,
      cell: ({ row }) =>
        row.original.arm === "holdout" ? (
          <div className="flex flex-col gap-0.5">
            <span className="text-xs text-foreground">{t("arm.holdout")}</span>
            <span className="text-[11px] text-muted-foreground">
              {t("holdoutNote")}
            </span>
          </div>
        ) : (
          <span className="text-xs text-foreground">
            {isDemo ? t("arm.treatmentDemo") : t("arm.treatment")}
          </span>
        ),
    },
    {
      id: "targeted",
      header: t("columns.targeted"),
      enableSorting: false,
      cell: ({ row }) => (
        <span className="text-xs text-foreground">
          {row.original.isTargeted ? t("targeted.yes") : t("targeted.no")}
        </span>
      ),
    },
    {
      id: "sendStatus",
      header: t("columns.sendStatus"),
      enableSorting: false,
      cell: ({ row }) =>
        row.original.sendStatus ? (
          <span className="text-xs text-foreground">
            {isDemo
              ? t("sendStatus.demo")
              : sendStatusLabel(row.original.sendStatus)}
          </span>
        ) : (
          <span className="text-muted-foreground/40">—</span>
        ),
    },
    {
      id: "conversion",
      header: t("columns.conversion"),
      enableSorting: false,
      cell: ({ row }) => (
        <div className="flex flex-col gap-0.5">
          <span className="text-xs text-foreground">
            {conversionLabel(row.original.conversion)}
          </span>
          {row.original.firstConversionAt ? (
            <span className="text-[11px] tabular-nums text-muted-foreground">
              {formatDateTime(row.original.firstConversionAt, locale)}
            </span>
          ) : null}
        </div>
      ),
    },
    {
      id: "revenue",
      header: t("columns.revenue"),
      enableSorting: false,
      cell: ({ row }) => {
        if (row.original.revenue === null) {
          return row.original.conversion === CONVERSION_NOT_VISIBLE ? (
            <span className="text-xs text-muted-foreground">
              {t("visibility.hidden")}
            </span>
          ) : (
            <span className="text-muted-foreground/40">—</span>
          );
        }
        return (
          <span className="text-xs tabular-nums text-foreground">
            {formatCurrency(toNumber(row.original.revenue), currency, locale)}
          </span>
        );
      },
    },
    {
      id: "email",
      header: t("columns.email"),
      enableSorting: false,
      cell: ({ row }) => {
        const email = recipientEmail(row.original);
        if (email.kind === "address") {
          return (
            <span className="font-mono text-[11px] text-muted-foreground">
              {email.address}
            </span>
          );
        }
        if (email.kind === "hidden") {
          return (
            <span className="text-xs text-muted-foreground">
              {t("email.hidden")}
            </span>
          );
        }
        return <span className="text-muted-foreground/40">—</span>;
      },
    },
  ];

  const hasActiveFilters = Boolean(
    tableState.q ||
    tableState.after ||
    arm ||
    targeted ||
    sendStatus ||
    converted,
  );

  return (
    <section aria-labelledby="recipients-title" className="space-y-4">
      <div>
        <h2
          id="recipients-title"
          className="text-base font-semibold text-foreground"
        >
          {t("title")}
        </h2>
        <p className="mt-0.5 text-xs text-muted-foreground">{t("subtitle")}</p>
      </div>

      <DataTable
        data={rows}
        columns={columns}
        getRowId={(row) => row.recipientId}
        loading={query.isPending && !query.data}
        isFetching={query.isFetching}
        manualPagination
        emptyTitle={scanning ? t("scanning.title") : t("empty")}
        emptyDescription={scanning ? t("scanning.description") : undefined}
        emptyAction={
          scanning ? (
            <Button
              variant="secondary"
              className="px-3 text-xs"
              onClick={() =>
                tableState.handleNextPage(query.data?.nextCursor ?? null)
              }
            >
              {t("scanning.loadMore")}
            </Button>
          ) : undefined
        }
        toolbar={
          <DataTableSearchFilterBar
            searchInput={tableState.searchInput}
            onSearchInputChange={tableState.setSearchInput}
            onClearSearch={tableState.clearSearch}
            searchPlaceholder={t("searchPlaceholder")}
            hasActiveFilters={hasActiveFilters}
            onResetFilters={() =>
              tableState.resetAll(() => {
                void setArm("");
                void setTargeted("");
                void setSendStatus("");
                void setConverted("");
              })
            }
            filters={[
              armFilter,
              targetedFilter,
              sendStatusFilter,
              convertedFilter,
            ]}
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
      {rows.some((row) => !row.emailVisible) ? (
        <p className="text-xs text-muted-foreground">{t("email.hiddenNote")}</p>
      ) : null}
      {rows.some(
        (row) => !row.nameVisible || row.conversion === CONVERSION_NOT_VISIBLE,
      ) ? (
        <p className="text-xs text-muted-foreground">
          {t("visibility.hiddenNote")}
        </p>
      ) : null}
    </section>
  );
}
