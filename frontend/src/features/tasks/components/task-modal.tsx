"use client";

import { Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { useWatch } from "react-hook-form";

import { useApiForm } from "@/components/forms/use-api-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input, Textarea } from "@/components/ui/input";
import { Modal, ModalFooter } from "@/components/ui/modal";
import { SegmentedControl } from "@/components/ui/segmented-control";
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
import {
  DEFAULT_TASK_TYPE,
  TASK_TYPES,
  TASK_TYPE_ICONS,
  TASK_TYPE_LABEL_KEYS,
  taskTypeOf,
} from "@/features/tasks/lib/task-types";
import type { Task } from "@/features/tasks/types";
import { useMe } from "@/lib/api/hooks/use-api";
import {
  displayTimeZone,
  localDateTimeStringToUtcIso,
  utcIsoToLocalDateTimeString,
} from "@/lib/time-zones";
import { showSuccessToast } from "@/lib/toasts";

const TASK_FORM_ID = "task-form";

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
      type: task ? taskTypeOf(task.type) : DEFAULT_TASK_TYPE,
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
      "type",
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
          type: data.type,
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
          type: data.type,
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

  const selectedType = useWatch({ control: form.control, name: "type" });
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
      <form id={TASK_FORM_ID} onSubmit={onSubmit} className="space-y-4">
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

        {/* Type */}
        <div className="space-y-1.5">
          <p className="text-xs font-medium text-foreground">
            {t("form.type")}
          </p>
          <SegmentedControl
            value={selectedType}
            ariaLabel={t("form.type")}
            options={TASK_TYPES.map((code) => {
              const Icon = TASK_TYPE_ICONS[code];
              return {
                value: code,
                label: t(TASK_TYPE_LABEL_KEYS[code]),
                icon: <Icon className="h-3.5 w-3.5" aria-hidden="true" />,
              };
            })}
            onChange={(next) => setValue("type", next, { shouldDirty: true })}
          />
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
        <ModalFooter>
          <Button
            type="submit"
            form={TASK_FORM_ID}
            variant="primary"
            className="w-full"
            disabled={isSubmitting || (isEdit && !isDirty)}
          >
            {isSubmitting ? (
              <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
            ) : null}
            {isEdit ? t("save") : t("create")}
          </Button>
        </ModalFooter>
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
  const t = useTranslations("crm.tasks");
  const { data: me } = useMe();
  const timeZone = displayTimeZone(me);
  const currentUserId = me?.user.id ?? "";
  const currentUserName = me?.user.displayName ?? null;

  return (
    <Modal
      open={open}
      onOpenChange={onOpenChange}
      title={task ? t("editTask") : t("newTask")}
      size="lg"
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
    </Modal>
  );
}
