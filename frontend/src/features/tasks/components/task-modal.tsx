"use client";

import * as Dialog from "@radix-ui/react-dialog";
import { Loader2, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { useWatch } from "react-hook-form";

import { useApiForm } from "@/components/forms/use-api-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input, Textarea } from "@/components/ui/input";
import { ERROR_CODES } from "@/constants/error-codes";
import { LIMITS } from "@/constants/limits";
import { CompanyPicker } from "@/features/crm/components/company-picker";
import { OwnerPicker } from "@/features/crm/components/owner-picker";
import { ContactPicker } from "@/features/deals/components/contact-picker";
import { DealPicker } from "@/features/deals/components/deal-picker";
import { useTaskMutations } from "@/features/tasks/hooks/use-task-mutations";
import {
  taskFormSchema,
  type TaskFormValues,
} from "@/features/tasks/schemas/task-schema";
import type { Task } from "@/features/tasks/types";
import { useMe } from "@/lib/api/hooks/use-api";
import {
  displayTimeZone,
  localDateTimeStringToUtcIso,
  utcIsoToLocalDateTimeString,
} from "@/lib/time-zones";
import { showSuccessToast } from "@/lib/toasts";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  task?: Task | null;
  initialSubject?: {
    contactId?: string;
    contactName?: string | null;
    dealId?: string;
    dealTitle?: string | null;
    companyId?: string;
    companyName?: string | null;
  };
  onSaved?: (task: Task) => void;
}

interface FormBodyProps {
  task?: Task | null;
  initialSubject?: {
    contactId?: string;
    contactName?: string | null;
    dealId?: string;
    dealTitle?: string | null;
    companyId?: string;
    companyName?: string | null;
  };
  timeZone: string;
  currentUserId: string;
  currentUserName: string | null;
  onClose: () => void;
  onSaved?: (task: Task) => void;
}

function TaskModalFormBody({
  task,
  initialSubject,
  timeZone,
  currentUserId,
  currentUserName,
  onClose,
  onSaved,
}: FormBodyProps) {
  const t = useTranslations("crm.tasks");
  const tCommon = useTranslations("common");
  const { createTask, updateTask } = useTaskMutations();
  const isEdit = Boolean(task);

  const [contactName, setContactName] = useState<string | null>(
    task?.contactName ?? initialSubject?.contactName ?? null,
  );
  const [dealTitle, setDealTitle] = useState<string | null>(
    task?.dealTitle ?? initialSubject?.dealTitle ?? null,
  );
  const [companyName, setCompanyName] = useState<string | null>(
    task?.companyName ?? initialSubject?.companyName ?? null,
  );
  const [assigneeName, setAssigneeName] = useState<string | null>(
    task?.assigneeDisplayName ?? currentUserName,
  );

  const { form, onSubmit, formError, alertRef } = useApiForm<TaskFormValues>({
    schema: taskFormSchema,
    defaultValues: {
      title: task?.title ?? "",
      notes: task?.notes ?? "",
      dueAt: task?.dueAt
        ? utcIsoToLocalDateTimeString(task.dueAt, timeZone)
        : "",
      assigneeUserId: task?.assigneeUserId ?? currentUserId,
      contactId: task?.contactId ?? initialSubject?.contactId ?? null,
      dealId: task?.dealId ?? initialSubject?.dealId ?? null,
      companyId: task?.companyId ?? initialSubject?.companyId ?? null,
    },
    fields: [
      "title",
      "notes",
      "dueAt",
      "assigneeUserId",
      "contactId",
      "dealId",
      "companyId",
    ],
    codeFields: {
      [ERROR_CODES.taskAssigneeNotAllowed]: "assigneeUserId",
      [ERROR_CODES.taskSubjectNotFound]: "contactId",
    },
    submit: async (data) => {
      const dueAtUtc = data.dueAt
        ? localDateTimeStringToUtcIso(data.dueAt, timeZone)
        : null;

      if (task) {
        const updated = await updateTask(task.id, {
          version: task.version,
          title: data.title,
          notes: data.notes || null,
          dueAt: dueAtUtc,
          assigneeUserId: data.assigneeUserId,
          contactId: data.contactId || null,
          dealId: data.dealId || null,
          companyId: data.companyId || null,
        });
        showSuccessToast(t("taskUpdated"));
        onSaved?.(updated);
      } else {
        const created = await createTask({
          title: data.title,
          notes: data.notes || null,
          dueAt: dueAtUtc,
          assigneeUserId: data.assigneeUserId,
          contactId: data.contactId || null,
          dealId: data.dealId || null,
          companyId: data.companyId || null,
        });
        showSuccessToast(t("taskCreated"));
        onSaved?.(created);
      }
      onClose();
    },
  });

  const {
    register,
    setValue,
    formState: { errors, isSubmitting, isDirty },
  } = form;

  const selectedAssignee = useWatch({
    control: form.control,
    name: "assigneeUserId",
  });
  const selectedContact = useWatch({
    control: form.control,
    name: "contactId",
  });
  const selectedDeal = useWatch({
    control: form.control,
    name: "dealId",
  });
  const selectedCompany = useWatch({
    control: form.control,
    name: "companyId",
  });

  return (
    <>
      <div className="flex items-center justify-between pb-4 border-b border-border">
        <Dialog.Title className="text-base font-semibold text-foreground">
          {isEdit ? t("editTask") : t("newTask")}
        </Dialog.Title>
        <Dialog.Close asChild>
          <button
            type="button"
            aria-label={tCommon("cancel")}
            className="rounded-xs p-1 text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          >
            <X className="h-4 w-4" />
          </button>
        </Dialog.Close>
      </div>

      <form onSubmit={onSubmit} className="mt-4 space-y-4">
        {formError ? (
          <Alert ref={alertRef} tone="error">
            {formError}
          </Alert>
        ) : null}

        {/* Title */}
        <div className="space-y-1.5">
          <label
            htmlFor="task-title"
            className="text-xs font-medium text-foreground"
          >
            {t("form.title")} <span className="text-destructive">*</span>
          </label>
          <Input
            id="task-title"
            {...register("title")}
            placeholder={t("form.titlePlaceholder")}
            maxLength={LIMITS.taskTitleMaxLength}
            autoFocus
          />
          {errors.title && (
            <p className="text-xs text-destructive">
              {t(`errors.${errors.title.message}` as Parameters<typeof t>[0])}
            </p>
          )}
        </div>

        {/* Due Date & Time */}
        <div className="space-y-1.5">
          <label
            htmlFor="task-due"
            className="text-xs font-medium text-foreground flex items-center gap-1"
          >
            <span>{t("form.dueDate")}</span>
            <span className="text-[11px] text-muted-foreground font-normal">
              ({timeZone})
            </span>
          </label>
          <Input
            id="task-due"
            type="datetime-local"
            {...register("dueAt")}
            className="text-xs"
          />
        </div>

        {/* Assignee */}
        <div className="space-y-1.5">
          <label className="text-xs font-medium text-foreground">
            {t("form.assignee")} <span className="text-destructive">*</span>
          </label>
          <OwnerPicker
            value={selectedAssignee}
            valueDisplayName={assigneeName}
            allowClear={false}
            onChange={(id, owner) => {
              setValue("assigneeUserId", id ?? "", { shouldDirty: true });
              setAssigneeName(owner?.displayName ?? null);
            }}
            placeholder={t("form.selectAssignee")}
          />
          {errors.assigneeUserId && (
            <p className="text-xs text-destructive">
              {t("errors.assigneeRequired")}
            </p>
          )}
        </div>

        {/* Linked Records: Contact, Deal, Company */}
        <div className="grid grid-cols-1 gap-4">
          <div className="space-y-1.5">
            <label className="text-xs font-medium text-foreground">
              {t("form.linkedContact")}
            </label>
            <ContactPicker
              value={selectedContact}
              valueName={contactName}
              onChange={(id: string | null, name?: string | null) => {
                setValue("contactId", id, { shouldDirty: true });
                setContactName(name ?? null);
              }}
              placeholder={t("form.selectContact")}
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-medium text-foreground">
              {t("form.linkedDeal")}
            </label>
            <DealPicker
              value={selectedDeal}
              valueTitle={dealTitle}
              onChange={(id: string | null, title?: string | null) => {
                setValue("dealId", id, { shouldDirty: true });
                setDealTitle(title ?? null);
              }}
              placeholder={t("form.selectDeal")}
            />
          </div>

          <div className="space-y-1.5">
            <label className="text-xs font-medium text-foreground">
              {t("form.linkedCompany")}
            </label>
            <CompanyPicker
              value={selectedCompany}
              valueName={companyName}
              onChange={(id, name) => {
                setValue("companyId", id, { shouldDirty: true });
                setCompanyName(name ?? null);
              }}
              placeholder={t("form.selectCompany")}
            />
          </div>
        </div>

        {/* Notes */}
        <div className="space-y-1.5 pt-1">
          <label
            htmlFor="task-notes"
            className="text-xs font-medium text-foreground"
          >
            {t("form.notes")}
          </label>
          <Textarea
            id="task-notes"
            {...register("notes")}
            rows={3}
            maxLength={LIMITS.taskNotesMaxLength}
            placeholder={t("form.notesPlaceholder")}
          />
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-2 pt-4 border-t border-border">
          <Button
            type="button"
            variant="secondary"
            onClick={onClose}
            disabled={isSubmitting}
          >
            {tCommon("cancel")}
          </Button>
          <Button
            type="submit"
            variant="primary"
            disabled={isSubmitting || (isEdit && !isDirty)}
          >
            {isSubmitting ? (
              <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
            ) : null}
            {isEdit ? t("save") : t("create")}
          </Button>
        </div>
      </form>
    </>
  );
}

export function TaskModal({
  open,
  onOpenChange,
  task,
  initialSubject,
  onSaved,
}: Props) {
  const { data: me } = useMe();
  const timeZone = displayTimeZone(me);
  const currentUserId = me?.user.id ?? "";
  const currentUserName = me?.user.displayName ?? null;

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/40 backdrop-blur-xs transition-opacity duration-150 data-[state=closed]:opacity-0 data-[state=open]:opacity-100" />
        <Dialog.Content
          aria-describedby={undefined}
          className="fixed left-1/2 top-1/2 z-50 w-full max-w-lg -translate-x-1/2 -translate-y-1/2 rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-[var(--shadow-floating)] max-h-[90vh] overflow-y-auto"
        >
          {open ? (
            <TaskModalFormBody
              key={`${task?.id ?? "new"}:${task?.version ?? 0}:${currentUserId}`}
              task={task}
              initialSubject={initialSubject}
              timeZone={timeZone}
              currentUserId={currentUserId}
              currentUserName={currentUserName}
              onClose={() => onOpenChange(false)}
              onSaved={onSaved}
            />
          ) : null}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
