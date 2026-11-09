"use client";

import * as Dialog from "@radix-ui/react-dialog";
import { Loader2, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useWatch } from "react-hook-form";

import { useApiForm } from "@/components/forms/use-api-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/input";
import { ERROR_CODES } from "@/constants/error-codes";
import { LIMITS } from "@/constants/limits";
import { useNoteMutations } from "@/features/timeline/hooks/use-note-mutations";
import {
  noteFormSchema,
  type NoteFormValues,
} from "@/features/timeline/schemas/note-schema";
import type { Note, SubjectRef } from "@/features/timeline/types";
import { showSuccessToast } from "@/lib/toasts";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  note?: Note | null;
  subject: SubjectRef;
  onSaved?: (note: Note) => void;
}

interface FormBodyProps {
  note?: Note | null;
  subject: SubjectRef;
  onClose: () => void;
  onSaved?: (note: Note) => void;
}

function NoteModalFormBody({ note, subject, onClose, onSaved }: FormBodyProps) {
  const t = useTranslations("crm.notes");
  const tCommon = useTranslations("common");
  const { createNote, updateNote } = useNoteMutations();
  const isEdit = Boolean(note);

  const { form, onSubmit, formError, alertRef } = useApiForm<NoteFormValues>({
    schema: noteFormSchema,
    defaultValues: {
      body: note?.body ?? "",
      contactId: note?.contactId ?? subject.contactId ?? null,
      dealId: note?.dealId ?? subject.dealId ?? null,
      companyId: note?.companyId ?? subject.companyId ?? null,
    },
    fields: ["body", "contactId", "dealId", "companyId"],
    codeFields: {
      [ERROR_CODES.concurrencyConflict]: "body",
    },
    submit: async (data) => {
      if (note) {
        const updated = await updateNote(
          note.id,
          {
            version: note.version,
            body: data.body,
          },
          subject,
        );
        showSuccessToast(t("noteUpdated"));
        onSaved?.(updated);
      } else {
        const created = await createNote(
          {
            body: data.body,
            contactId: subject.contactId || null,
            dealId: subject.dealId || null,
            companyId: subject.companyId || null,
          },
          subject,
        );
        showSuccessToast(t("noteCreated"));
        onSaved?.(created);
      }
      onClose();
    },
  });

  const {
    register,
    formState: { errors, isSubmitting, isDirty },
  } = form;

  const currentBody =
    useWatch({
      control: form.control,
      name: "body",
    }) || "";

  return (
    <>
      <div className="flex items-center justify-between pb-4 border-b border-border">
        <Dialog.Title className="text-base font-semibold text-foreground">
          {isEdit ? t("editNote") : t("addNote")}
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

        <div className="space-y-1.5">
          <label
            htmlFor="note-body"
            className="text-xs font-medium text-foreground"
          >
            {t("form.body")} <span className="text-destructive">*</span>
          </label>
          <Textarea
            id="note-body"
            {...register("body")}
            rows={5}
            maxLength={LIMITS.noteBodyMaxLength}
            placeholder={t("form.bodyPlaceholder")}
            autoFocus
          />
          <div className="flex items-center justify-between text-[11px] text-muted-foreground">
            {errors.body ? (
              <span className="text-destructive">
                {t(`errors.${errors.body.message}` as Parameters<typeof t>[0])}
              </span>
            ) : (
              <span />
            )}
            <span>
              {currentBody.length} / {LIMITS.noteBodyMaxLength}
            </span>
          </div>
        </div>

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

export function NoteModal({
  open,
  onOpenChange,
  note,
  subject,
  onSaved,
}: Props) {
  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/40 backdrop-blur-xs transition-opacity duration-150 data-[state=closed]:opacity-0 data-[state=open]:opacity-100" />
        <Dialog.Content
          aria-describedby={undefined}
          className="fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-[var(--shadow-floating)] max-h-[90vh] overflow-y-auto"
        >
          {open ? (
            <NoteModalFormBody
              key={`${note?.id ?? "new"}:${note?.version ?? 0}`}
              note={note}
              subject={subject}
              onClose={() => onOpenChange(false)}
              onSaved={onSaved}
            />
          ) : null}
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
