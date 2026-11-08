"use client";

import { Archive, Edit2, Plus } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { toast } from "sonner";

import { FormField } from "@/components/forms/form-field";
import { useApiForm } from "@/components/forms/use-api-form";
import { Alert } from "@/components/ui/alert";
import { AlertDialog } from "@/components/ui/alert-dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PERMISSIONS } from "@/constants/permissions";
import { useCustomFieldMutations } from "@/features/crm/hooks/use-custom-field-mutations";
import { useCustomFields } from "@/features/crm/hooks/use-custom-fields";
import {
  createCustomFieldSchema,
  updateCustomFieldSchema,
  type CreateCustomFieldValues,
  type UpdateCustomFieldValues,
} from "@/features/crm/schemas/custom-field-schema";
import { Skeleton } from "@/components/ui/skeleton";
import type { CustomFieldDefinition } from "@/features/crm/types";
import { Can, useCanStatus } from "@/features/session/components/can";
import * as Dialog from "@radix-ui/react-dialog";

export function CustomFieldsSettingsPanel() {
  const t = useTranslations("settings.customFields");
  const tCommon = useTranslations("common");
  const tValidation = useTranslations("validation");

  const canManageStatus = useCanStatus(PERMISSIONS.customFieldsManage);

  const [activeEntityType, setActiveEntityType] = useState<
    "contact" | "company"
  >("contact");
  const [createOpen, setCreateOpen] = useState(false);
  const [editingField, setEditingField] =
    useState<CustomFieldDefinition | null>(null);
  const [archivingField, setArchivingField] =
    useState<CustomFieldDefinition | null>(null);

  const customFieldsQuery = useCustomFields(activeEntityType);
  const { createCustomField, updateCustomField, archiveCustomField } =
    useCustomFieldMutations();

  const definitions = customFieldsQuery.data ?? [];
  const activeFields = definitions.filter((d) => !d.archivedAt);
  const archivedFields = definitions.filter((d) => Boolean(d.archivedAt));

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
      entityType: activeEntityType,
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

      toast.success(t("saved"));
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

      toast.success(t("saved"));
      setEditingField(null);
    },
  });

  if (canManageStatus === "pending") {
    return (
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div className="space-y-2">
            <Skeleton className="h-6 w-48" />
            <Skeleton className="h-4 w-72" />
          </div>
          <Skeleton className="h-8 w-32" />
        </div>
        <Skeleton className="h-9 w-40" />
        <div className="rounded-[var(--radius-card)] border border-border bg-card p-4 space-y-3">
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
        </div>
      </div>
    );
  }

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
    toast.success(t("archivedSuccess"));
    setArchivingField(null);
  }

  const selectedFieldType = createForm.watch("fieldType");

  return (
    <div className="space-y-6">
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
                entityType: activeEntityType,
                fieldType: "text",
                options: "",
                isRequired: false,
                position: activeFields.length,
              });
              setCreateOpen(true);
            }}
            className="h-8 gap-1.5 text-xs px-3 self-start sm:self-auto"
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
          onClick={() => setActiveEntityType("contact")}
          className={`px-3 py-1.5 text-xs font-medium rounded-sm transition-colors cursor-pointer ${
            activeEntityType === "contact"
              ? "bg-primary text-primary-foreground shadow-sm"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          {t("entityType.contact")}
        </button>
        <button
          type="button"
          onClick={() => setActiveEntityType("company")}
          className={`px-3 py-1.5 text-xs font-medium rounded-sm transition-colors cursor-pointer ${
            activeEntityType === "company"
              ? "bg-primary text-primary-foreground shadow-sm"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          {t("entityType.company")}
        </button>
      </div>

      {/* Custom Fields List */}
      <div className="rounded-[var(--radius-card)] border border-border bg-card overflow-hidden">
        {activeFields.length === 0 && archivedFields.length === 0 ? (
          <div className="p-8 text-center text-xs text-muted-foreground">
            {t("empty", { entityType: t(`entityType.${activeEntityType}`) })}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs border-collapse">
              <thead className="border-b border-border bg-muted/50 font-semibold text-muted-foreground">
                <tr>
                  <th className="py-2.5 px-4">{t("fields.label")}</th>
                  <th className="py-2.5 px-4">{t("fields.key")}</th>
                  <th className="py-2.5 px-4">{t("fields.fieldType")}</th>
                  <th className="py-2.5 px-4">{t("fields.isRequired")}</th>
                  <th className="py-2.5 px-4">{t("fields.position")}</th>
                  <th className="py-2.5 px-4 text-right">{t("actions")}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {activeFields.map((field) => (
                  <tr
                    key={field.id}
                    className="hover:bg-muted/30 transition-colors"
                  >
                    <td className="py-2.5 px-4 font-medium text-foreground">
                      {field.label}
                    </td>
                    <td className="py-2.5 px-4 font-mono text-[11px] text-muted-foreground">
                      {field.key}
                    </td>
                    <td className="py-2.5 px-4">
                      <span className="inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-medium bg-muted text-muted-foreground uppercase">
                        {t(`types.${field.fieldType}`)}
                      </span>
                    </td>
                    <td className="py-2.5 px-4">
                      {field.isRequired ? (
                        <span className="text-primary font-medium">✓</span>
                      ) : (
                        <span className="text-muted-foreground/40">—</span>
                      )}
                    </td>
                    <td className="py-2.5 px-4 text-muted-foreground">
                      {field.position}
                    </td>
                    <td className="py-2.5 px-4 text-right">
                      <div className="inline-flex items-center gap-1">
                        <Can permission={PERMISSIONS.customFieldsManage}>
                          <Button
                            variant="secondary"
                            onClick={() => startEdit(field)}
                            className="h-6 w-6 p-0"
                            aria-label={t("editField", { label: field.label })}
                          >
                            <Edit2 className="h-3 w-3 text-muted-foreground" />
                          </Button>
                          <Button
                            variant="secondary"
                            onClick={() => setArchivingField(field)}
                            className="h-6 w-6 p-0 text-destructive hover:bg-destructive/10"
                            aria-label={t("archiveField", {
                              label: field.label,
                            })}
                          >
                            <Archive className="h-3 w-3" />
                          </Button>
                        </Can>
                      </div>
                    </td>
                  </tr>
                ))}

                {archivedFields.map((field) => (
                  <tr
                    key={field.id}
                    className="bg-muted/30 text-muted-foreground"
                  >
                    <td className="py-2.5 px-4 line-through">{field.label}</td>
                    <td className="py-2.5 px-4 font-mono text-[11px]">
                      {field.key}
                    </td>
                    <td className="py-2.5 px-4">
                      <span className="inline-flex items-center rounded px-1.5 py-0.5 text-[10px] font-medium bg-muted text-muted-foreground uppercase">
                        {t(`types.${field.fieldType}`)}
                      </span>
                    </td>
                    <td className="py-2.5 px-4">
                      {field.isRequired ? (
                        <span className="font-medium">✓</span>
                      ) : (
                        <span className="opacity-40">—</span>
                      )}
                    </td>
                    <td className="py-2.5 px-4">{field.position}</td>
                    <td className="py-2.5 px-4 text-right">
                      <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground bg-muted px-1.5 py-0.5 rounded">
                        {t("archived")}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Create Dialog */}
      <Dialog.Root open={createOpen} onOpenChange={setCreateOpen}>
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
                  placeholder="e.g. Lead Source"
                  className="h-8 text-xs bg-background"
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
                  placeholder="e.g. lead_source"
                  className="h-8 font-mono text-xs bg-background"
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
                  <SelectTrigger className="h-8 text-xs bg-background">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {(
                      ["text", "number", "boolean", "select", "date"] as const
                    ).map((ft) => (
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
                    placeholder="Web, Partner, Referral, Event"
                    className="h-8 text-xs bg-background"
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
                  className="h-8 text-xs bg-background"
                />
              </FormField>

              <div className="flex justify-end gap-2 pt-2">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setCreateOpen(false)}
                  className="h-8 px-3 text-xs"
                >
                  {tCommon("cancel")}
                </Button>
                <Button
                  type="submit"
                  variant="primary"
                  disabled={createForm.formState.isSubmitting}
                  className="h-8 px-3 text-xs"
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
                  className="h-8 text-xs bg-background"
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
                    className="h-8 text-xs bg-background"
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
                  className="h-8 text-xs bg-background"
                />
              </FormField>

              <div className="flex justify-end gap-2 pt-2">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => setEditingField(null)}
                  className="h-8 px-3 text-xs"
                >
                  {tCommon("cancel")}
                </Button>
                <Button
                  type="submit"
                  variant="primary"
                  disabled={editForm.formState.isSubmitting}
                  className="h-8 px-3 text-xs"
                >
                  {tCommon("save")}
                </Button>
              </div>
            </form>
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>

      {/* Archive confirmation dialog */}
      <AlertDialog
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
