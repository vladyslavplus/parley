"use client";

import * as Dialog from "@radix-ui/react-dialog";
import type { ColumnDef } from "@tanstack/react-table";
import { Archive, Edit2, Plus } from "lucide-react";
import { useTranslations } from "next-intl";
import { parseAsString, parseAsStringLiteral, useQueryState } from "nuqs";
import { useState } from "react";

import {
  DataTable,
  DataTableSearchFilterBar,
  useServerTableState,
} from "@/components/data-table";
import { FormField } from "@/components/forms/form-field";
import { useApiForm } from "@/components/forms/use-api-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
  SimpleSelect,
} from "@/components/ui/select";
import { PERMISSIONS } from "@/constants/permissions";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import { useCustomFieldMutations } from "@/features/crm/hooks/use-custom-field-mutations";
import {
  type CustomFieldsPageParams,
  useCustomFieldsPage,
} from "@/features/crm/hooks/use-custom-fields";
import {
  createCustomFieldSchema,
  updateCustomFieldSchema,
  type CreateCustomFieldValues,
  type UpdateCustomFieldValues,
} from "@/features/crm/schemas/custom-field-schema";
import type { CustomFieldDefinition } from "@/features/crm/types";
import { Can, useCanStatus } from "@/features/session/components/can";
import { showSuccessToast } from "@/lib/toasts";
import { cn } from "@/lib/utils";

const FIELD_TYPES = ["text", "number", "boolean", "select", "date"] as const;

export function CustomFieldsSettingsPanel() {
  const t = useTranslations("settings.customFields");
  const tCommon = useTranslations("common");
  const tValidation = useTranslations("validation");

  const canManageStatus = useCanStatus(PERMISSIONS.customFieldsManage);

  const [entityType, setEntityType] = useQueryState(
    "entityType",
    parseAsStringLiteral(["contact", "company"] as const).withDefault(
      "contact",
    ),
  );
  const [fieldType, setFieldType] = useQueryState(
    "fieldType",
    parseAsString.withDefault(""),
  );

  const tableState = useServerTableState({
    defaultSort: "name",
    defaultOrder: "asc",
    allowedSorts: ["name", "position", "createdAt"] as const,
  });

  const [createOpen, setCreateOpen] = useState(false);
  const [editingField, setEditingField] =
    useState<CustomFieldDefinition | null>(null);
  const [archivingField, setArchivingField] =
    useState<CustomFieldDefinition | null>(null);

  const customFieldsQuery = useCustomFieldsPage(
    {
      after: tableState.after,
      limit: tableState.pageSize,
      q: tableState.q || undefined,
      entityType,
      fieldType:
        (fieldType as CustomFieldsPageParams["fieldType"]) || undefined,
      sort: tableState.sort,
      order: tableState.order,
    },
    canManageStatus !== "denied",
  );
  const { createCustomField, updateCustomField, archiveCustomField } =
    useCustomFieldMutations();

  const definitions = customFieldsQuery.data?.items ?? [];
  const activeCount = definitions.filter((d) => !d.archivedAt).length;

  const createSchema = createCustomFieldSchema((k, v) => tValidation(k, v));
  const updateSchema = updateCustomFieldSchema((k, v) => tValidation(k, v));

  // Create Form
  const {
    form: createForm,
    onSubmit: onCreateSubmit,
    formError: createFormError,
    alertRef: createAlertRef,
  } = useApiForm<CreateCustomFieldValues>({
    schema: createSchema,
    defaultValues: {
      key: "",
      label: "",
      entityType,
      fieldType: "text",
      options: "",
      isRequired: false,
      position: 0,
    },
    fields: [
      "key",
      "label",
      "entityType",
      "fieldType",
      "options",
      "isRequired",
      "position",
    ],
    submit: async (values) => {
      const optionsArray =
        values.fieldType === "select" && values.options
          ? values.options
              .split(",")
              .map((s) => s.trim())
              .filter(Boolean)
          : null;

      await createCustomField({
        entityType: values.entityType,
        key: values.key,
        label: values.label,
        fieldType: values.fieldType,
        options: optionsArray,
        isRequired: values.isRequired,
        position: values.position,
      });

      showSuccessToast(t("saved"));
      createForm.reset();
      setCreateOpen(false);
    },
  });

  // Edit Form
  const {
    form: editForm,
    onSubmit: onEditSubmit,
    formError: editFormError,
    alertRef: editAlertRef,
  } = useApiForm<UpdateCustomFieldValues>({
    schema: updateSchema,
    defaultValues: {
      label: editingField?.label ?? "",
      options: editingField?.options?.join(", ") ?? "",
      isRequired: editingField?.isRequired ?? false,
      position: Number(editingField?.position ?? 0),
    },
    fields: ["label", "options", "isRequired", "position"],
    submit: async (values) => {
      if (!editingField) return;

      const optionsArray =
        editingField.fieldType === "select" && values.options
          ? values.options
              .split(",")
              .map((s) => s.trim())
              .filter(Boolean)
          : null;

      await updateCustomField(editingField.id, {
        label: values.label,
        options: optionsArray,
        isRequired: values.isRequired,
        position: values.position,
      });

      showSuccessToast(t("saved"));
      setEditingField(null);
    },
  });

  if (canManageStatus === "denied") {
    return (
      <div className="rounded-[var(--radius-card)] border border-border bg-card p-8 text-center text-sm text-muted-foreground">
        {tCommon("forbidden")}
      </div>
    );
  }

  function startEdit(field: CustomFieldDefinition) {
    setEditingField(field);
    editForm.reset({
      label: field.label,
      options: field.options?.join(", ") ?? "",
      isRequired: field.isRequired,
      position: Number(field.position ?? 0),
    });
  }

  async function handleArchive() {
    if (!archivingField) return;
    await archiveCustomField(archivingField.id);
    showSuccessToast(t("archivedSuccess"));
    setArchivingField(null);
  }

  const selectedFieldType = createForm.watch("fieldType");

  const columns: ColumnDef<CustomFieldDefinition, unknown>[] = [
    {
      id: "name",
      accessorKey: "label",
      header: t("fields.label"),
      enableSorting: true,
      cell: ({ row }) => {
        const field = row.original;
        const archived = Boolean(field.archivedAt);
        return (
          <span
            className={cn(
              "font-medium text-xs",
              archived
                ? "line-through text-muted-foreground"
                : "text-foreground",
            )}
          >
            {field.label}
          </span>
        );
      },
    },
    {
      id: "key",
      accessorKey: "key",
      header: t("fields.key"),
      enableSorting: false,
      cell: ({ row }) => (
        <span className="font-mono text-[11px] text-muted-foreground">
          {row.original.key}
        </span>
      ),
    },
    {
      id: "fieldType",
      accessorKey: "fieldType",
      header: t("fields.fieldType"),
      enableSorting: false,
      cell: ({ row }) => (
        <span className="inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-medium bg-muted text-muted-foreground uppercase">
          {t(`types.${row.original.fieldType}`)}
        </span>
      ),
    },
    {
      id: "isRequired",
      accessorKey: "isRequired",
      header: t("fields.isRequired"),
      enableSorting: false,
      cell: ({ row }) =>
        row.original.isRequired ? (
          <span className="text-primary font-medium">✓</span>
        ) : (
          <span className="text-muted-foreground/40">—</span>
        ),
    },
    {
      id: "position",
      accessorKey: "position",
      header: t("fields.position"),
      enableSorting: true,
      cell: ({ row }) => (
        <span className="text-xs text-muted-foreground">
          {row.original.position}
        </span>
      ),
    },
    {
      id: "actions",
      header: () => <span className="block text-right">{t("actions")}</span>,
      enableSorting: false,
      cell: ({ row }) => {
        const field = row.original;
        const archived = Boolean(field.archivedAt);
        if (archived) {
          return (
            <div className="flex justify-end">
              <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground bg-muted px-1.5 py-0.5 rounded">
                {t("archived")}
              </span>
            </div>
          );
        }
        return (
          <div className="flex items-center justify-end gap-1">
            <Can permission={PERMISSIONS.customFieldsManage}>
              <Button
                variant="secondary"
                onClick={() => startEdit(field)}
                className="h-6 w-6 p-0"
                title={t("editField", { label: field.label })}
                aria-label={t("editField", { label: field.label })}
              >
                <Edit2 className="h-3 w-3 text-muted-foreground" />
              </Button>
              <Button
                variant="secondary"
                onClick={() => setArchivingField(field)}
                className="h-6 w-6 p-0 text-destructive hover:bg-destructive/10"
                title={t("archiveField", { label: field.label })}
                aria-label={t("archiveField", { label: field.label })}
              >
                <Archive className="h-3 w-3" />
              </Button>
            </Can>
          </div>
        );
      },
    },
  ];

  const hasActiveFilters = Boolean(
    tableState.q || tableState.after || fieldType,
  );

  return (
    <div className="space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <p className="text-xs text-muted-foreground">{t("subtitle")}</p>
        </div>

        <Can permission={PERMISSIONS.customFieldsManage}>
          <Button
            variant="primary"
            onClick={() => {
              createForm.reset({
                key: "",
                label: "",
                entityType,
                fieldType: "text",
                options: "",
                isRequired: false,
                position: activeCount,
              });
              setCreateOpen(true);
            }}
            className="gap-1.5 text-xs px-3 self-start sm:self-auto whitespace-nowrap"
          >
            <Plus className="h-3.5 w-3.5" />
            <span>{t("create")}</span>
          </Button>
        </Can>
      </div>

      {/* Entity Type Switcher */}
      <div className="inline-flex rounded-[var(--radius-control)] border border-border p-1 bg-card">
        <button
          type="button"
          onClick={() => {
            if (entityType !== "contact") {
              void setEntityType("contact");
              tableState.resetPaging();
            }
          }}
          className={cn(
            "px-3 py-1.5 text-xs font-medium rounded-sm transition-colors cursor-pointer",
            entityType === "contact"
              ? "bg-primary text-primary-foreground shadow-sm"
              : "text-muted-foreground hover:text-foreground",
          )}
        >
          {t("entityType.contact")}
        </button>
        <button
          type="button"
          onClick={() => {
            if (entityType !== "company") {
              void setEntityType("company");
              tableState.resetPaging();
            }
          }}
          className={cn(
            "px-3 py-1.5 text-xs font-medium rounded-sm transition-colors cursor-pointer",
            entityType === "company"
              ? "bg-primary text-primary-foreground shadow-sm"
              : "text-muted-foreground hover:text-foreground",
          )}
        >
          {t("entityType.company")}
        </button>
      </div>

      {/* Custom Fields Table */}
      <DataTable
        data={definitions}
        columns={columns}
        getRowId={(row) => row.id}
        loading={
          (canManageStatus === "pending" || customFieldsQuery.isPending) &&
          !customFieldsQuery.data
        }
        isFetching={customFieldsQuery.isFetching}
        manualPagination
        manualSorting
        sorting={tableState.sorting}
        onSortingChange={tableState.onSortingChange}
        emptyTitle={t("empty", {
          entityType: t(`entityType.${entityType}`),
        })}
        toolbar={
          <DataTableSearchFilterBar
            searchInput={tableState.searchInput}
            onSearchInputChange={tableState.setSearchInput}
            onClearSearch={tableState.clearSearch}
            searchPlaceholder={t("searchPlaceholder")}
            hasActiveFilters={hasActiveFilters}
            onResetFilters={() =>
              tableState.resetAll(() => {
                void setFieldType("");
              })
            }
          >
            <SimpleSelect
              ariaLabel={t("fields.fieldType")}
              value={fieldType}
              onValueChange={(next) => {
                void setFieldType(next);
                tableState.resetPaging();
              }}
              options={[
                { value: "", label: t("filters.allTypes") },
                ...FIELD_TYPES.map((ft) => ({
                  value: ft,
                  label: t(`types.${ft}`),
                })),
              ]}
              className="w-auto min-w-[140px] text-xs"
            />
          </DataTableSearchFilterBar>
        }
        paginationSlot={
          <KeysetPagination
            hasPreviousPage={tableState.hasPreviousPage}
            hasNextPage={Boolean(customFieldsQuery.data?.nextCursor)}
            onPreviousPage={tableState.handlePreviousPage}
            onNextPage={() =>
              tableState.handleNextPage(
                customFieldsQuery.data?.nextCursor ?? null,
              )
            }
            pageSize={tableState.pageSize}
            onPageSizeChange={tableState.setPageSize}
            itemCount={definitions.length}
          />
        }
      />

      {/* Create Dialog */}
      <Dialog.Root
        open={createOpen}
        onOpenChange={(nextOpen) => {
          if (!nextOpen) {
            createForm.reset({
              key: "",
              label: "",
              entityType,
              fieldType: "text",
              options: "",
              isRequired: false,
              position: activeCount,
            });
          }
          setCreateOpen(nextOpen);
        }}
      >
        <Dialog.Portal>
          <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/30 backdrop-blur-sm animate-in fade-in-0 duration-150" />
          <Dialog.Content className="fixed left-[50%] top-[50%] z-50 w-full max-w-md translate-x-[-50%] translate-y-[-50%] rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-floating focus:outline-none">
            <Dialog.Title className="text-base font-semibold text-foreground">
              {t("create")}
            </Dialog.Title>
            <Dialog.Description className="sr-only">
              {t("subtitle")}
            </Dialog.Description>

            <form onSubmit={onCreateSubmit} className="space-y-4 pt-4">
              {createFormError ? (
                <Alert ref={createAlertRef}>{createFormError}</Alert>
              ) : null}

              <FormField
                label={t("fields.label")}
                error={createForm.formState.errors.label?.message}
                required
              >
                <Input
                  {...createForm.register("label")}
                  placeholder={t("fields.labelPlaceholder")}
                  className="text-xs bg-background"
                />
              </FormField>

              <FormField
                label={t("fields.key")}
                hint={t("fields.keyHelp")}
                error={createForm.formState.errors.key?.message}
                required
              >
                <Input
                  {...createForm.register("key")}
                  placeholder={t("fields.keyPlaceholder")}
                  className="font-mono text-xs bg-background"
                />
              </FormField>

              <FormField
                label={t("fields.fieldType")}
                error={createForm.formState.errors.fieldType?.message}
                required
              >
                <Select
                  value={createForm.watch("fieldType")}
                  onValueChange={(val) =>
                    createForm.setValue(
                      "fieldType",
                      val as CreateCustomFieldValues["fieldType"],
                    )
                  }
                >
                  <SelectTrigger className="text-xs bg-background">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {FIELD_TYPES.map((ft) => (
                      <SelectItem key={ft} value={ft}>
                        {t(`types.${ft}`)}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </FormField>

              {selectedFieldType === "select" && (
                <FormField
                  label={t("fields.options")}
                  hint={t("fields.optionsHelp")}
                  error={createForm.formState.errors.options?.message}
                >
                  <Input
                    {...createForm.register("options")}
                    placeholder={t("fields.optionsPlaceholder")}
                    className="text-xs bg-background"
                  />
                </FormField>
              )}

              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="create-isRequired"
                  {...createForm.register("isRequired")}
                  className="h-4 w-4 rounded border-input text-primary focus:ring-ring"
                />
                <label
                  htmlFor="create-isRequired"
                  className="text-xs font-medium text-foreground cursor-pointer"
                >
                  {t("fields.isRequired")}
                </label>
              </div>

              <FormField
                label={t("fields.position")}
                error={createForm.formState.errors.position?.message}
              >
                <Input
                  type="number"
                  {...createForm.register("position", { valueAsNumber: true })}
                  className="text-xs bg-background"
                />
              </FormField>

              <div className="flex justify-end gap-2 pt-2">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setCreateOpen(false)}
                  className="px-3 text-xs"
                >
                  {tCommon("cancel")}
                </Button>
                <Button
                  type="submit"
                  variant="primary"
                  disabled={createForm.formState.isSubmitting}
                  className="px-3 text-xs"
                >
                  {tCommon("save")}
                </Button>
              </div>
            </form>
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>

      {/* Edit Dialog */}
      <Dialog.Root
        open={Boolean(editingField)}
        onOpenChange={(open) => !open && setEditingField(null)}
      >
        <Dialog.Portal>
          <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/30 backdrop-blur-sm animate-in fade-in-0 duration-150" />
          <Dialog.Content className="fixed left-[50%] top-[50%] z-50 w-full max-w-md translate-x-[-50%] translate-y-[-50%] rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-floating focus:outline-none">
            <Dialog.Title className="text-base font-semibold text-foreground">
              {t("edit")}
            </Dialog.Title>
            <Dialog.Description className="sr-only">
              {editingField?.key}
            </Dialog.Description>

            <form onSubmit={onEditSubmit} className="space-y-4 pt-4">
              {editFormError ? (
                <Alert ref={editAlertRef}>{editFormError}</Alert>
              ) : null}

              <FormField
                label={t("fields.label")}
                error={editForm.formState.errors.label?.message}
                required
              >
                <Input
                  {...editForm.register("label")}
                  className="text-xs bg-background"
                />
              </FormField>

              {editingField?.fieldType === "select" && (
                <FormField
                  label={t("fields.options")}
                  hint={t("fields.optionsHelp")}
                  error={editForm.formState.errors.options?.message}
                >
                  <Input
                    {...editForm.register("options")}
                    className="text-xs bg-background"
                  />
                </FormField>
              )}

              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="edit-isRequired"
                  {...editForm.register("isRequired")}
                  className="h-4 w-4 rounded border-input text-primary focus:ring-ring"
                />
                <label
                  htmlFor="edit-isRequired"
                  className="text-xs font-medium text-foreground cursor-pointer"
                >
                  {t("fields.isRequired")}
                </label>
              </div>

              <FormField
                label={t("fields.position")}
                error={editForm.formState.errors.position?.message}
              >
                <Input
                  type="number"
                  {...editForm.register("position", { valueAsNumber: true })}
                  className="text-xs bg-background"
                />
              </FormField>

              <div className="flex justify-end gap-2 pt-2">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setEditingField(null)}
                  className="px-3 text-xs"
                >
                  {tCommon("cancel")}
                </Button>
                <Button
                  type="submit"
                  variant="primary"
                  disabled={
                    editForm.formState.isSubmitting ||
                    !editForm.formState.isDirty
                  }
                  className="px-3 text-xs"
                >
                  {tCommon("save")}
                </Button>
              </div>
            </form>
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>

      {/* Archive confirmation dialog */}
      <ConfirmDialog
        open={Boolean(archivingField)}
        onOpenChange={(open) => !open && setArchivingField(null)}
        title={t("archiveConfirm")}
        description={t("archiveConfirmDescription")}
        confirmLabel={t("archive")}
        onConfirm={handleArchive}
        destructive
      />
    </div>
  );
}
