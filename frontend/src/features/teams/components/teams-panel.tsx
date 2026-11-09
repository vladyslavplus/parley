"use client";

import * as Dialog from "@radix-ui/react-dialog";
import type { ColumnDef } from "@tanstack/react-table";
import { Edit2, Plus, Trash2 } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import {
  DataTable,
  DataTableSearchFilterBar,
  useServerTableState,
} from "@/components/data-table";
import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { useApiForm } from "@/components/forms/use-api-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/input";
import { queryKeys } from "@/constants/query-keys";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import {
  createTeamSchema,
  type TeamValues,
} from "@/features/teams/schemas/team-schema";
import { backendVoid } from "@/lib/api/bff-client";
import { useInvalidate, useTeamsPage } from "@/lib/api/hooks/use-api";
import type { Schemas } from "@/lib/api/schemas";
import { showSuccessToast } from "@/lib/toasts";

function TeamModalDialog({
  open,
  onOpenChange,
  title,
  description,
  initialName,
  requireDirty,
  submitLabel,
  onSubmitName,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: string;
  initialName: string;
  requireDirty?: boolean;
  submitLabel: string;
  onSubmitName: (name: string) => Promise<void>;
}) {
  const t = useTranslations("settings.teams");
  const tCommon = useTranslations("common");
  const tValidation = useTranslations("validation");
  const schema = createTeamSchema(tValidation);

  const { form, onSubmit, formError, alertRef } = useApiForm<TeamValues>({
    schema,
    defaultValues: { name: initialName },
    fields: ["name"],
    submit: async (values) => {
      await onSubmitName(values.name);
      form.reset({ name: "" });
      onOpenChange(false);
    },
  });

  const [prevInitialName, setPrevInitialName] = useState(initialName);
  const [prevOpen, setPrevOpen] = useState(open);
  if (open !== prevOpen || initialName !== prevInitialName) {
    setPrevOpen(open);
    setPrevInitialName(initialName);
    if (open) {
      form.reset({ name: initialName });
    }
  }

  const { errors, isSubmitting, isDirty } = form.formState;

  function handleOpenChange(nextOpen: boolean) {
    if (!nextOpen) {
      form.reset({ name: initialName });
    }
    onOpenChange(nextOpen);
  }

  return (
    <Dialog.Root open={open} onOpenChange={handleOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/30 backdrop-blur-sm animate-in fade-in-0 duration-150" />
        <Dialog.Content className="fixed left-[50%] top-[50%] z-50 w-full max-w-md translate-x-[-50%] translate-y-[-50%] rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-floating space-y-4 focus:outline-none">
          <div>
            <Dialog.Title className="text-base font-semibold text-foreground">
              {title}
            </Dialog.Title>
            <Dialog.Description className="mt-1 text-xs text-muted-foreground">
              {description}
            </Dialog.Description>
          </div>

          <ApiForm
            onSubmit={onSubmit}
            formError={formError}
            alertRef={alertRef}
            pending={isSubmitting}
            submitDisabled={requireDirty ? !isDirty : false}
            submitLabel={submitLabel}
          >
            <FormField label={t("name")} error={errors.name?.message}>
              <Input
                className="text-xs bg-background"
                {...form.register("name")}
              />
            </FormField>
            <div className="flex justify-end pt-1">
              <Button
                type="button"
                variant="secondary"
                onClick={() => handleOpenChange(false)}
                disabled={isSubmitting}
                className="px-3 text-xs"
              >
                {tCommon("cancel")}
              </Button>
            </div>
          </ApiForm>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}

export function TeamsPanel() {
  const t = useTranslations("settings.teams");
  const locale = useLocale();
  const invalidate = useInvalidate();

  const tableState = useServerTableState({
    defaultSort: "name",
    defaultOrder: "asc",
    allowedSorts: ["name", "createdAt"] as const,
  });

  const [createOpen, setCreateOpen] = useState(false);
  const [editingTeam, setEditingTeam] = useState<Schemas["TeamView"] | null>(
    null,
  );
  const [deletingTeam, setDeletingTeam] = useState<Schemas["TeamView"] | null>(
    null,
  );

  const teamsQuery = useTeamsPage({
    after: tableState.after,
    limit: tableState.pageSize,
    q: tableState.q || undefined,
    sort: tableState.sort,
    order: tableState.order,
  });

  async function handleCreateTeam(name: string) {
    await backendVoid("/teams", { method: "POST", body: { name }, locale });
    await Promise.all([
      invalidate(queryKeys.teams()),
      invalidate(queryKeys.members()),
    ]);
    showSuccessToast(t("createModal.created"));
  }

  async function handleRenameTeam(name: string) {
    if (!editingTeam) return;
    await backendVoid(
      "/teams/{id}",
      { method: "PUT", body: { name }, locale },
      `/teams/${editingTeam.id}`,
    );
    await Promise.all([
      invalidate(queryKeys.teams()),
      invalidate(queryKeys.members()),
    ]);
    showSuccessToast(t("editModal.saved"));
    setEditingTeam(null);
  }

  async function handleDeleteTeam() {
    if (!deletingTeam) return;
    await backendVoid(
      "/teams/{id}",
      { method: "DELETE", locale },
      `/teams/${deletingTeam.id}`,
    );
    await Promise.all([
      invalidate(queryKeys.teams()),
      invalidate(queryKeys.members()),
    ]);
    showSuccessToast(t("deleteModal.deleted"));
    setDeletingTeam(null);
  }

  const rows = teamsQuery.data?.items ?? [];

  const columns: ColumnDef<Schemas["TeamView"], unknown>[] = [
    {
      id: "name",
      accessorKey: "name",
      header: t("columns.name"),
      enableSorting: true,
      cell: ({ row }) => (
        <span className="font-medium text-foreground text-xs">
          {row.original.name}
        </span>
      ),
    },
    {
      id: "id",
      accessorKey: "id",
      header: t("columns.id"),
      enableSorting: false,
      cell: ({ row }) => (
        <span className="font-mono text-[11px] text-muted-foreground">
          {row.original.id}
        </span>
      ),
    },
    {
      id: "actions",
      header: () => <span className="sr-only">{t("columns.actions")}</span>,
      enableSorting: false,
      cell: ({ row }) => {
        const team = row.original;
        return (
          <div className="flex items-center justify-end gap-1">
            <Button
              variant="secondary"
              onClick={() => setEditingTeam(team)}
              className="h-6 w-6 p-0"
              title={t("editTeam", { name: team.name })}
              aria-label={t("editTeam", { name: team.name })}
            >
              <Edit2 className="h-3 w-3 text-muted-foreground" />
            </Button>
            <Button
              variant="secondary"
              onClick={() => setDeletingTeam(team)}
              className="h-6 w-6 p-0 text-destructive hover:bg-destructive/10"
              title={t("deleteTeam", { name: team.name })}
              aria-label={t("deleteTeam", { name: team.name })}
            >
              <Trash2 className="h-3 w-3" />
            </Button>
          </div>
        );
      },
    },
  ];

  const hasActiveFilters = Boolean(tableState.q || tableState.after);

  return (
    <div className="space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <p className="text-xs text-muted-foreground">{t("subtitle")}</p>
        <Button
          variant="primary"
          onClick={() => setCreateOpen(true)}
          className="gap-1.5 text-xs px-3 self-start sm:self-auto whitespace-nowrap"
        >
          <Plus className="h-3.5 w-3.5" />
          <span>{t("create")}</span>
        </Button>
      </div>

      {teamsQuery.isError && !teamsQuery.data ? (
        <Alert>{t("loadFailed")}</Alert>
      ) : (
        <DataTable
          data={rows}
          columns={columns}
          getRowId={(row) => row.id}
          loading={teamsQuery.isPending && !teamsQuery.data}
          isFetching={teamsQuery.isFetching}
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
              onResetFilters={() => tableState.resetAll()}
            />
          }
          paginationSlot={
            <KeysetPagination
              hasPreviousPage={tableState.hasPreviousPage}
              hasNextPage={Boolean(teamsQuery.data?.nextCursor)}
              onPreviousPage={tableState.handlePreviousPage}
              onNextPage={() =>
                tableState.handleNextPage(teamsQuery.data?.nextCursor ?? null)
              }
              pageSize={tableState.pageSize}
              onPageSizeChange={tableState.setPageSize}
              itemCount={rows.length}
            />
          }
        />
      )}

      {/* Create Team Modal */}
      <TeamModalDialog
        open={createOpen}
        onOpenChange={setCreateOpen}
        title={t("createModal.title")}
        description={t("createModal.description")}
        initialName=""
        submitLabel={t("create")}
        onSubmitName={handleCreateTeam}
      />

      {/* Edit Team Modal */}
      <TeamModalDialog
        open={Boolean(editingTeam)}
        onOpenChange={(open) => !open && setEditingTeam(null)}
        title={t("editModal.title")}
        description={t("editModal.description")}
        initialName={editingTeam?.name ?? ""}
        requireDirty
        submitLabel={t("save")}
        onSubmitName={handleRenameTeam}
      />

      {/* Delete Team Confirmation Dialog */}
      <ConfirmDialog
        open={Boolean(deletingTeam)}
        onOpenChange={(open) => !open && setDeletingTeam(null)}
        title={t("deleteModal.title")}
        description={t("deleteModal.description", {
          name: deletingTeam?.name ?? "",
        })}
        confirmLabel={t("delete")}
        onConfirm={handleDeleteTeam}
        destructive
      />
    </div>
  );
}
