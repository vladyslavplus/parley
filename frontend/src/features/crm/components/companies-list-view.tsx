"use client";

import type { ColumnDef } from "@tanstack/react-table";
import { ExternalLink, Plus, RefreshCw, Search, X } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { parseAsString, parseAsStringLiteral, useQueryState } from "nuqs";
import { useEffect, useState } from "react";

import {
  DataTable,
  DataTableColumnsMenu,
  DataTableDensityToggle,
  useKeysetPaging,
} from "@/components/data-table";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import { PERMISSIONS } from "@/constants/permissions";
import { companyRecordRoute } from "@/constants/routes";
import { CompanyPreviewDrawer } from "@/features/crm/components/company-preview-drawer";
import { CompanyQuickCreateDialog } from "@/features/crm/components/company-quick-create-dialog";
import { CrmBulkBar } from "@/features/crm/components/crm-bulk-bar";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import { OwnerPicker } from "@/features/crm/components/owner-picker";
import { useCompanies } from "@/features/crm/hooks/use-companies";
import { useCrmMutations } from "@/features/crm/hooks/use-crm-mutations";
import type { Company } from "@/features/crm/types";
import { Can } from "@/features/session/components/can";
import { Link } from "@/i18n/navigation";
import { formatDate } from "@/lib/formatters";
import { showSuccessToast, useUndoToast } from "@/lib/toasts";

export function CompaniesListView() {
  const t = useTranslations("crm.companies");
  const tCrm = useTranslations("crm");
  const tFilters = useTranslations("crm.filters");
  const locale = useLocale();
  const undoToast = useUndoToast();

  const [q, setQ] = useQueryState("q", parseAsString.withDefault(""));
  const [searchInput, setSearchInput] = useState(q);
  const [prevQ, setPrevQ] = useState(q);
  if (prevQ !== q) {
    setPrevQ(q);
    setSearchInput(q);
  }

  const [sort, setSort] = useQueryState(
    "sort",
    parseAsString.withDefault("name"),
  );
  const [order, setOrder] = useQueryState(
    "order",
    parseAsStringLiteral(["asc", "desc"] as const).withDefault("asc"),
  );
  const [ownerUserId, setOwnerUserId] = useQueryState(
    "ownerUserId",
    parseAsString.withDefault(""),
  );

  const {
    after,
    setAfter,
    pageSize,
    setPageSize,
    hasPreviousPage,
    handleNextPage,
    handlePreviousPage,
    resetPaging,
  } = useKeysetPaging();

  const [selectedCompany, setSelectedCompany] = useState<Company | null>(null);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [quickCreateOpen, setQuickCreateOpen] = useState(false);

  // Debounce search input writes to URL
  useEffect(() => {
    const timer = setTimeout(() => {
      const trimmed = searchInput.trim();
      const nextQ =
        trimmed.length >= LIMITS.searchMinQueryLength ? trimmed : "";
      if (nextQ !== q) {
        void setQ(nextQ);
        void setAfter("");
      }
    }, LIMITS.searchDebounceMs);

    return () => clearTimeout(timer);
  }, [searchInput, q, setQ, setAfter]);

  const companiesQuery = useCompanies({
    q: q || undefined,
    sort: sort || undefined,
    order: order || undefined,
    ownerUserId: ownerUserId || undefined,
    after: after || undefined,
    limit: pageSize,
  });

  const { bulkAssignCompanyOwner, bulkDeleteCompanies, restoreCompany } =
    useCrmMutations();

  const companies = companiesQuery.data?.items ?? [];
  const nextCursor = companiesQuery.data?.nextCursor ?? null;

  function handleRowClick(row: Company) {
    setSelectedCompany(row);
    setPreviewOpen(true);
  }

  function handleFilterReset() {
    setSearchInput("");
    void setQ("");
    void setOwnerUserId("");
    resetPaging();
  }

  const columns: ColumnDef<Company, unknown>[] = [
    {
      id: "select",
      header: ({ table }) => (
        <input
          type="checkbox"
          checked={table.getIsAllPageRowsSelected()}
          onChange={(e) => table.toggleAllPageRowsSelected(e.target.checked)}
          aria-label={tCrm("table.pageCount", { count: companies.length })}
          className="h-3.5 w-3.5 rounded border-input text-primary focus:ring-ring"
        />
      ),
      cell: ({ row }) => (
        <input
          type="checkbox"
          checked={row.getIsSelected()}
          onChange={(e) => row.toggleSelected(e.target.checked)}
          aria-label={row.original.name}
          className="h-3.5 w-3.5 rounded border-input text-primary focus:ring-ring"
        />
      ),
      enableSorting: false,
      enableHiding: false,
    },
    {
      id: "name",
      accessorKey: "name",
      header: t("columns.name"),
      enableSorting: true,
      cell: ({ row }) => {
        const company = row.original;
        return (
          <Link
            href={companyRecordRoute(company.id)}
            onClick={(e) => e.stopPropagation()}
            className="font-medium text-foreground hover:underline truncate text-xs"
          >
            {company.name}
          </Link>
        );
      },
    },
    {
      id: "domain",
      accessorKey: "domain",
      header: t("columns.domain"),
      enableSorting: false,
      cell: ({ row }) => {
        const domain = row.original.domain;
        if (!domain) return <span className="text-muted-foreground/40">—</span>;
        const websiteUrl = domain.startsWith("http")
          ? domain
          : `https://${domain}`;
        return (
          <div className="flex items-center gap-1.5">
            <span className="text-xs text-muted-foreground truncate">
              {domain}
            </span>
            <a
              href={websiteUrl}
              target="_blank"
              rel="noreferrer"
              onClick={(e) => e.stopPropagation()}
              className="text-muted-foreground hover:text-foreground"
              aria-label={t("preview.visitWebsite")}
            >
              <ExternalLink className="h-3 w-3" />
            </a>
          </div>
        );
      },
    },
    {
      id: "country",
      accessorKey: "country",
      header: t("columns.country"),
      enableSorting: false,
      cell: ({ row }) =>
        row.original.country ? (
          <span className="text-xs font-mono uppercase text-muted-foreground">
            {row.original.country}
          </span>
        ) : (
          <span className="text-muted-foreground/40">—</span>
        ),
    },
    {
      id: "industry",
      accessorKey: "industry",
      header: t("columns.industry"),
      enableSorting: false,
      cell: ({ row }) =>
        row.original.industry ? (
          <span className="text-xs text-muted-foreground truncate">
            {row.original.industry}
          </span>
        ) : (
          <span className="text-muted-foreground/40">—</span>
        ),
    },
    {
      id: "owner",
      header: t("columns.owner"),
      enableSorting: false,
      cell: ({ row }) => {
        const ownerName =
          row.original.ownerDisplayName ?? row.original.ownerEmail;
        if (!ownerName)
          return <span className="text-muted-foreground/40">—</span>;
        return (
          <span className="text-xs text-muted-foreground truncate">
            {ownerName}
          </span>
        );
      },
    },
    {
      id: "created",
      accessorKey: "createdAt",
      header: t("columns.created"),
      enableSorting: true,
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground whitespace-nowrap">
          {formatDate(row.original.createdAt ?? "", locale)}
        </span>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      {/* Header with Title and Quick Create */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">
            {t("title")}
          </h1>
          <p className="text-xs text-muted-foreground">{t("subtitle")}</p>
        </div>

        <Can permission={PERMISSIONS.companiesWrite}>
          <Button
            variant="primary"
            onClick={() => setQuickCreateOpen(true)}
            className="h-8 gap-1.5 text-xs px-3 self-start sm:self-auto"
          >
            <Plus className="h-3.5 w-3.5" />
            <span>{t("create")}</span>
          </Button>
        </Can>
      </div>

      {/* Main Data Table */}
      <DataTable
        data={companies}
        columns={columns}
        getRowId={(row) => row.id}
        loading={companiesQuery.isPending && !companiesQuery.data}
        isFetching={companiesQuery.isFetching}
        manualPagination
        manualSorting
        onRowClick={handleRowClick}
        toolbar={
          <div className="flex flex-wrap items-center justify-between gap-3 w-full">
            <div className="flex flex-wrap items-center gap-2 flex-1 min-w-[280px]">
              {/* Search input with debounce */}
              <div className="relative w-full max-w-xs">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-3.5 w-3.5 text-muted-foreground pointer-events-none" />
                <Input
                  value={searchInput}
                  onChange={(e) => setSearchInput(e.target.value)}
                  placeholder={t("searchPlaceholder")}
                  className="h-8 pl-8 text-xs bg-background"
                />
                {searchInput && (
                  <button
                    type="button"
                    onClick={() => {
                      setSearchInput("");
                      void setQ("");
                      resetPaging();
                    }}
                    className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground cursor-pointer"
                    aria-label={tFilters("clearSearch")}
                  >
                    <X className="h-3.5 w-3.5" />
                  </button>
                )}
              </div>

              {/* Owner filter */}
              <OwnerPicker
                variant="filter"
                value={ownerUserId || null}
                valueDisplayName={
                  ownerUserId
                    ? (companies.find((r) => r.ownerUserId === ownerUserId)
                        ?.ownerDisplayName ?? null)
                    : null
                }
                valueEmail={
                  ownerUserId
                    ? (companies.find((r) => r.ownerUserId === ownerUserId)
                        ?.ownerEmail ?? null)
                    : null
                }
                onChange={(nextOwnerId) => {
                  void setOwnerUserId(nextOwnerId ?? "");
                  resetPaging();
                }}
              />

              {(q || ownerUserId || after) && (
                <Button
                  variant="secondary"
                  onClick={handleFilterReset}
                  className="h-8 px-2.5 text-xs text-muted-foreground"
                >
                  {tFilters("reset")}
                </Button>
              )}
            </div>

            <div className="flex items-center gap-2">
              <DataTableDensityToggle />
              <DataTableColumnsMenu />
            </div>
          </div>
        }
        sorting={
          sort
            ? [{ id: sort, desc: order === "desc" }]
            : [{ id: "name", desc: false }]
        }
        onSortingChange={(updater) => {
          const next =
            typeof updater === "function"
              ? updater(
                  sort
                    ? [{ id: sort, desc: order === "desc" }]
                    : [{ id: "name", desc: false }],
                )
              : updater;
          if (next.length > 0) {
            void setSort(next[0].id === "created" ? "created" : "name");
            void setOrder(next[0].desc ? "desc" : "asc");
          } else {
            void setSort("name");
            void setOrder("asc");
          }
          resetPaging();
        }}
        emptyTitle={companiesQuery.isError ? t("loadError") : t("empty.title")}
        emptyDescription={
          companiesQuery.isError ? undefined : t("empty.description")
        }
        emptyAction={
          companiesQuery.isError ? (
            <div className="flex items-center justify-center gap-2">
              <Button
                variant="secondary"
                onClick={() => void companiesQuery.refetch()}
                className="h-8 gap-1.5 text-xs px-3"
              >
                <RefreshCw className="h-3.5 w-3.5" />
                <span>{t("retry")}</span>
              </Button>
              {after && (
                <Button
                  variant="secondary"
                  onClick={resetPaging}
                  className="h-8 gap-1.5 text-xs px-3"
                >
                  <span>{t("resetPaging")}</span>
                </Button>
              )}
            </div>
          ) : q || ownerUserId || after ? (
            <Button
              variant="secondary"
              onClick={handleFilterReset}
              className="h-8 px-3 text-xs"
            >
              {tFilters("reset")}
            </Button>
          ) : (
            <Can permission={PERMISSIONS.companiesWrite}>
              <Button
                variant="primary"
                onClick={() => setQuickCreateOpen(true)}
                className="h-8 gap-1.5 text-xs px-3"
              >
                <Plus className="h-3.5 w-3.5" />
                <span>{t("create")}</span>
              </Button>
            </Can>
          )
        }
        paginationSlot={
          <KeysetPagination
            hasPreviousPage={hasPreviousPage}
            hasNextPage={Boolean(nextCursor)}
            onPreviousPage={handlePreviousPage}
            onNextPage={() => handleNextPage(nextCursor)}
            pageSize={pageSize}
            onPageSizeChange={setPageSize}
            itemCount={companies.length}
          />
        }
        bulkActions={(selectedRows, clearSelection) => (
          <CrmBulkBar
            selectedCount={selectedRows.length}
            writePermission={PERMISSIONS.companiesWrite}
            deletePermission={PERMISSIONS.companiesDelete}
            onAssignOwner={async (ownerId) => {
              const ids = selectedRows.map((r) => r.id);
              await bulkAssignCompanyOwner(ids, ownerId);
              showSuccessToast(tCrm("bulk.assigned", { count: ids.length }));
            }}
            onDelete={async () => {
              const ids = selectedRows.map((r) => r.id);
              await bulkDeleteCompanies(ids);
              undoToast({
                message: tCrm("bulk.deleted", { count: ids.length }),
                onUndo: async () => {
                  for (const id of ids) {
                    await restoreCompany(id);
                  }
                },
              });
            }}
            clearSelection={clearSelection}
          />
        )}
      />

      {/* Row Preview Drawer */}
      <CompanyPreviewDrawer
        company={selectedCompany}
        open={previewOpen}
        onOpenChange={setPreviewOpen}
      />

      {/* Quick Create Dialog */}
      <CompanyQuickCreateDialog
        open={quickCreateOpen}
        onOpenChange={setQuickCreateOpen}
      />
    </div>
  );
}
