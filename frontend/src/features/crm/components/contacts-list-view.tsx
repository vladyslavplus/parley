"use client";

import type { ColumnDef } from "@tanstack/react-table";
import { Download, Plus, RefreshCw } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { parseAsString, parseAsStringLiteral, useQueryState } from "nuqs";
import { useEffect, useState } from "react";

import {
  DataTable,
  DataTableColumnsMenu,
  DataTableDensityToggle,
  DataTableSearchFilterBar,
  useKeysetPaging,
} from "@/components/data-table";
import { Button } from "@/components/ui/button";
import { LIMITS } from "@/constants/limits";
import { PERMISSIONS } from "@/constants/permissions";
import { companyRecordRoute, contactRecordRoute } from "@/constants/routes";
import { ContactExportDialog } from "@/features/crm/components/contact-export-dialog";
import { ContactPreviewDrawer } from "@/features/crm/components/contact-preview-drawer";
import { ContactQuickCreateDialog } from "@/features/crm/components/contact-quick-create-dialog";
import { CrmBulkBar } from "@/features/crm/components/crm-bulk-bar";
import { CrmTagBadge } from "@/features/crm/components/crm-tag-badge";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import { useContacts } from "@/features/crm/hooks/use-contacts";
import {
  useOwnerDimension,
  useTagDimension,
} from "@/features/crm/filters/use-filter-dimensions";
import { useCrmMutations } from "@/features/crm/hooks/use-crm-mutations";
import { useTags } from "@/features/crm/hooks/use-tags";
import type { Contact } from "@/features/crm/types";
import { Can } from "@/features/session/components/can";
import { Link } from "@/i18n/navigation";
import { formatDate } from "@/lib/formatters";
import { showSuccessToast, useUndoToast } from "@/lib/toasts";

export function ContactsListView() {
  const t = useTranslations("crm.contacts");
  const tCrm = useTranslations("crm");
  const tFilters = useTranslations("crm.filters");
  const tTable = useTranslations("table");
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
  const [tagId, setTagId] = useQueryState(
    "tagId",
    parseAsString.withDefault(""),
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

  const [selectedContact, setSelectedContact] = useState<Contact | null>(null);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [quickCreateOpen, setQuickCreateOpen] = useState(false);
  const [exportDialogOpen, setExportDialogOpen] = useState(false);

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

  const contactsQuery = useContacts({
    q: q || undefined,
    sort: sort || undefined,
    order: order || undefined,
    tagId: tagId || undefined,
    ownerUserId: ownerUserId || undefined,
    after: after || undefined,
    limit: pageSize,
  });

  const tagsQuery = useTags();
  const { bulkAssignContactOwner, bulkDeleteContacts, restoreContact } =
    useCrmMutations();

  const tags = tagsQuery.data?.items ?? [];
  const tagMap = new Map(tags.map((tg) => [tg.id, tg]));

  const contacts = contactsQuery.data?.items ?? [];
  const nextCursor = contactsQuery.data?.nextCursor ?? null;

  function handleRowClick(row: Contact) {
    setSelectedContact(row);
    setPreviewOpen(true);
  }

  const tagDimension = useTagDimension({
    value: tagId,
    onChange: (value) => {
      void setTagId(value);
      resetPaging();
    },
  });
  const ownerDimension = useOwnerDimension({
    value: ownerUserId,
    onChange: (value) => {
      void setOwnerUserId(value);
      resetPaging();
    },
  });

  function handleFilterReset() {
    setSearchInput("");
    void setQ("");
    void setTagId("");
    void setOwnerUserId("");
    resetPaging();
  }

  const columns: ColumnDef<Contact, unknown>[] = [
    {
      id: "select",
      header: ({ table }) => (
        <input
          type="checkbox"
          checked={table.getIsAllPageRowsSelected()}
          onChange={(e) => table.toggleAllPageRowsSelected(e.target.checked)}
          aria-label={tTable("selection.selectAll")}
          className="h-3.5 w-3.5 rounded border-input text-primary focus:ring-ring"
        />
      ),
      cell: ({ row }) => (
        <input
          type="checkbox"
          checked={row.getIsSelected()}
          onChange={(e) => row.toggleSelected(e.target.checked)}
          aria-label={tTable("selection.selectRow")}
          className="h-3.5 w-3.5 rounded border-input text-primary focus:ring-ring"
        />
      ),
      enableSorting: false,
      enableHiding: false,
    },
    {
      id: "name",
      accessorFn: (row) => `${row.firstName} ${row.lastName ?? ""}`.trim(),
      header: t("columns.name"),
      enableSorting: true,
      cell: ({ row }) => {
        const contact = row.original;
        const fullName =
          `${contact.firstName} ${contact.lastName ?? ""}`.trim();
        return (
          <div className="flex flex-col">
            <Link
              href={contactRecordRoute(contact.id)}
              onClick={(e) => e.stopPropagation()}
              className="font-medium text-foreground hover:underline text-xs"
            >
              {fullName}
            </Link>
            {contact.jobTitle && (
              <span className="text-[11px] text-muted-foreground truncate">
                {contact.jobTitle}
              </span>
            )}
          </div>
        );
      },
    },
    {
      id: "email",
      accessorKey: "email",
      header: t("columns.email"),
      enableSorting: false,
      cell: ({ row }) =>
        row.original.email ? (
          <span className="text-xs font-mono text-muted-foreground truncate">
            {row.original.email}
          </span>
        ) : (
          <span className="text-muted-foreground/40">—</span>
        ),
    },
    {
      id: "phone",
      accessorKey: "phone",
      header: t("columns.phone"),
      enableSorting: false,
      cell: ({ row }) =>
        row.original.phone ? (
          <span className="text-xs font-mono text-muted-foreground truncate">
            {row.original.phone}
          </span>
        ) : (
          <span className="text-muted-foreground/40">—</span>
        ),
    },
    {
      id: "company",
      header: t("columns.company"),
      enableSorting: false,
      cell: ({ row }) => {
        const companyName = row.original.companyName;
        const companyId = row.original.companyId;
        if (!companyName && !companyId)
          return <span className="text-muted-foreground/40">—</span>;
        if (companyId) {
          return (
            <Link
              href={companyRecordRoute(companyId)}
              onClick={(e) => e.stopPropagation()}
              className="text-xs font-medium text-info hover:underline truncate"
            >
              {companyName || t("record.fields.linkedCompany")}
            </Link>
          );
        }
        return (
          <span className="text-xs text-foreground truncate">
            {companyName}
          </span>
        );
      },
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
      id: "tags",
      header: t("columns.tags"),
      enableSorting: false,
      cell: ({ row }) => {
        const tagIds = row.original.tagIds ?? [];
        if (tagIds.length === 0)
          return <span className="text-muted-foreground/40">—</span>;
        const contactTags = tagIds
          .map((id) => tagMap.get(id))
          .filter(Boolean)
          .slice(0, 3);
        return (
          <div className="flex flex-wrap items-center gap-1">
            {contactTags.map((tg) =>
              tg ? (
                <CrmTagBadge
                  key={tg.id}
                  name={tg.name}
                  colorIndex={tg.colorIndex}
                />
              ) : null,
            )}
            {tagIds.length > 3 && (
              <span className="text-[10px] text-muted-foreground">
                +{tagIds.length - 3}
              </span>
            )}
          </div>
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

        <Can permission={PERMISSIONS.contactsWrite}>
          <Button
            variant="primary"
            onClick={() => setQuickCreateOpen(true)}
            className="gap-1.5 self-start px-3 text-xs sm:self-auto"
          >
            <Plus className="h-4 w-4" />
            <span>{t("create")}</span>
          </Button>
        </Can>
      </div>

      {/* Main Data Table */}
      <DataTable
        data={contacts}
        columns={columns}
        getRowId={(row) => row.id}
        loading={contactsQuery.isPending && !contactsQuery.data}
        isFetching={contactsQuery.isFetching}
        manualPagination
        manualSorting
        onRowClick={handleRowClick}
        toolbar={
          <DataTableSearchFilterBar
            searchInput={searchInput}
            onSearchInputChange={setSearchInput}
            onClearSearch={() => {
              setSearchInput("");
              void setQ("");
              resetPaging();
            }}
            searchPlaceholder={t("searchPlaceholder")}
            filters={[tagDimension, ownerDimension]}
            hasActiveFilters={Boolean(q || tagId || ownerUserId || after)}
            onResetFilters={handleFilterReset}
            actions={
              <>
                <Can permission={PERMISSIONS.contactsExport}>
                  <Button
                    variant="secondary"
                    onClick={() => setExportDialogOpen(true)}
                    className="gap-1.5 px-2.5 text-xs"
                  >
                    <Download className="h-3.5 w-3.5 text-muted-foreground" />
                    <span>{tCrm("export.button")}</span>
                  </Button>
                </Can>
                <DataTableDensityToggle />
                <DataTableColumnsMenu />
              </>
            }
          />
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
        emptyTitle={contactsQuery.isError ? t("loadError") : t("empty.title")}
        emptyDescription={
          contactsQuery.isError ? undefined : t("empty.description")
        }
        emptyAction={
          contactsQuery.isError ? (
            <div className="flex items-center justify-center gap-2">
              <Button
                variant="secondary"
                onClick={() => void contactsQuery.refetch()}
                className="gap-1.5 px-3 text-xs"
              >
                <RefreshCw className="h-3.5 w-3.5" />
                <span>{t("retry")}</span>
              </Button>
              {after && (
                <Button
                  variant="secondary"
                  onClick={resetPaging}
                  className="gap-1.5 px-3 text-xs"
                >
                  <span>{t("resetPaging")}</span>
                </Button>
              )}
            </div>
          ) : q || tagId || ownerUserId || after ? (
            <Button
              variant="secondary"
              onClick={handleFilterReset}
              className="px-3 text-xs"
            >
              {tFilters("reset")}
            </Button>
          ) : (
            <Can permission={PERMISSIONS.contactsWrite}>
              <Button
                variant="primary"
                onClick={() => setQuickCreateOpen(true)}
                className="gap-1.5 px-3 text-xs"
              >
                <Plus className="h-4 w-4" />
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
            itemCount={contacts.length}
          />
        }
        bulkActions={(selectedRows, clearSelection) => (
          <CrmBulkBar
            selectedCount={selectedRows.length}
            writePermission={PERMISSIONS.contactsWrite}
            deletePermission={PERMISSIONS.contactsDelete}
            onAssignOwner={async (ownerId) => {
              const ids = selectedRows.map((r) => r.id);
              await bulkAssignContactOwner(ids, ownerId);
              showSuccessToast(tCrm("bulk.assigned", { count: ids.length }));
            }}
            onDelete={async () => {
              const ids = selectedRows.map((r) => r.id);
              await bulkDeleteContacts(ids);
              undoToast({
                message: tCrm("bulk.deleted", { count: ids.length }),
                onUndo: async () => {
                  for (const id of ids) {
                    await restoreContact(id);
                  }
                },
              });
            }}
            clearSelection={clearSelection}
          />
        )}
      />

      {/* Row Preview Drawer */}
      <ContactPreviewDrawer
        contact={selectedContact}
        open={previewOpen}
        onOpenChange={setPreviewOpen}
      />

      {/* Quick Create Dialog */}
      <ContactQuickCreateDialog
        open={quickCreateOpen}
        onOpenChange={setQuickCreateOpen}
      />

      {/* Contact Export Dialog */}
      <ContactExportDialog
        open={exportDialogOpen}
        onOpenChange={setExportDialogOpen}
        filters={{
          q: q || undefined,
          tagId: tagId || undefined,
          ownerUserId: ownerUserId || undefined,
        }}
      />
    </div>
  );
}
