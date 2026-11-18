"use client";

import { Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";
import { useWatch } from "react-hook-form";

import { useApiForm } from "@/components/forms/use-api-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/input";
import { Modal, ModalFooter } from "@/components/ui/modal";
import { ERROR_CODES } from "@/constants/error-codes";
import { LIMITS } from "@/constants/limits";
import { useNoteMutations } from "@/features/timeline/hooks/use-note-mutations";
import {
  noteFormSchema,
  type NoteFormValues,
} from "@/features/timeline/schemas/note-schema";
import type { Note, SubjectRef } from "@/features/timeline/types";
import { showSuccessToast } from "@/lib/toasts";

const NOTE_FORM_ID = "note-form";

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
      <form id={NOTE_FORM_ID} onSubmit={onSubmit} className="space-y-4">
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

        <ModalFooter>
          <Button
            type="submit"
            form={NOTE_FORM_ID}
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

export function NoteModal({
  open,
  onOpenChange,
  note,
  subject,
  onSaved,
}: Props) {
  const t = useTranslations("crm.notes");
  return (
    <Modal
      open={open}
      onOpenChange={onOpenChange}
      title={note ? t("editNote") : t("addNote")}
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
    </Modal>
  );
}
