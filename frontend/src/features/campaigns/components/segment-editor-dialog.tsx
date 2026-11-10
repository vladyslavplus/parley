"use client";

import { Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { useWatch } from "react-hook-form";

import { FormField } from "@/components/forms/form-field";
import { useApiForm } from "@/components/forms/use-api-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import { LibraryDialog } from "@/features/campaigns/components/library-dialog";
import { SegmentCount } from "@/features/campaigns/components/segment-count";
import { SegmentFilterFields } from "@/features/campaigns/components/segment-filter-fields";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { useSegment } from "@/features/campaigns/hooks/use-library";
import { useLibraryMutations } from "@/features/campaigns/hooks/use-library-mutations";
import {
  definitionFromValues,
  valuesFromDefinition,
} from "@/features/campaigns/lib/segment-definition";
import {
  segmentFormSchema,
  type SegmentFormValues,
} from "@/features/campaigns/schemas/segment-schema";
import type { Segment } from "@/features/campaigns/types";
import { showSuccessToast } from "@/lib/toasts";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  segmentId?: string | null;
  onSaved?: (segment: Segment) => void;
}

function EditorBody({
  initial,
  onClose,
  onSaved,
}: {
  initial: Segment | null;
  onClose: () => void;
  onSaved?: (segment: Segment) => void;
}) {
  const t = useTranslations("campaigns.segments");
  const tCommon = useTranslations("common");
  const tValidation = useTranslations("validation");
  const { createSegment, updateSegment } = useLibraryMutations();
  const [saved, setSaved] = useState<Segment | null>(initial);
  const rfmSegments = initial?.definition.rfmSegments ?? [];

  const { form, onSubmit, formError, alertRef } = useApiForm<SegmentFormValues>(
    {
      schema: segmentFormSchema(tValidation),
      defaultValues: valuesFromDefinition(
        initial?.name ?? "",
        initial?.definition,
      ),
      fields: [
        "name",
        "ownerUserIds",
        "countryCodes",
        "tagIds",
        "daysSinceLastOrder",
      ],
      submit: async (values) => {
        const body = {
          name: values.name,
          definition: definitionFromValues(values, rfmSegments),
        };
        const result = saved
          ? await updateSegment(saved.id, { ...body, version: saved.version })
          : await createSegment({ ...body, version: null });
        setSaved(result);
        showSuccessToast(t(saved ? "updated" : "created"));
        onSaved?.(result);
        onClose();
      },
    },
  );

  const {
    register,
    formState: { errors, isSubmitting, isDirty },
  } = form;
  const owners = useWatch({ control: form.control, name: "ownerUserIds" });
  const countries = useWatch({ control: form.control, name: "countryCodes" });
  const tagIds = useWatch({ control: form.control, name: "tagIds" });
  const days = useWatch({ control: form.control, name: "daysSinceLastOrder" });
  const definition = definitionFromValues(
    {
      ownerUserIds: owners,
      countryCodes: countries,
      tagIds,
      daysSinceLastOrder: days ?? "",
    },
    rfmSegments,
  );

  return (
    <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
      {formError ? (
        <Alert ref={alertRef} tone="error">
          {formError}
        </Alert>
      ) : null}

      <FormField label={t("form.name")} error={errors.name?.message} required>
        <Input
          {...register("name")}
          maxLength={LIMITS.segmentNameMaxLength}
          placeholder={t("form.namePlaceholder")}
          autoFocus
        />
      </FormField>

      <SegmentFilterFields form={form} rfmSegments={rfmSegments} />

      <SegmentCount definition={definition} />

      <div className="flex justify-end gap-2 border-t border-border pt-4">
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
          disabled={isSubmitting || (!!initial && !isDirty)}
        >
          {isSubmitting ? (
            <Loader2 className="h-3.5 w-3.5 animate-spin" />
          ) : null}
          {initial ? t("save") : t("create")}
        </Button>
      </div>
    </form>
  );
}

function LoadedEditor({
  segmentId,
  onClose,
  onSaved,
}: {
  segmentId: string | null;
  onClose: () => void;
  onSaved?: (segment: Segment) => void;
}) {
  const query = useSegment(segmentId);
  const errorText = useErrorText();

  if (segmentId && query.error) {
    return <Alert tone="error">{errorText(query.error)}</Alert>;
  }
  if (segmentId && !query.data) {
    return (
      <div
        aria-busy="true"
        className="h-64 animate-pulse rounded-[var(--radius-card)] bg-muted motion-reduce:animate-none"
      />
    );
  }
  return (
    <EditorBody
      key={query.data?.id ?? "new"}
      initial={query.data ?? null}
      onClose={onClose}
      onSaved={onSaved}
    />
  );
}

export function SegmentEditorDialog({
  open,
  onOpenChange,
  segmentId = null,
  onSaved,
}: Props) {
  const t = useTranslations("campaigns.segments");
  return (
    <LibraryDialog
      open={open}
      onOpenChange={onOpenChange}
      title={segmentId ? t("editTitle") : t("newTitle")}
      description={t("dialogDescription")}
    >
      <LoadedEditor
        segmentId={segmentId}
        onClose={() => onOpenChange(false)}
        onSaved={onSaved}
      />
    </LibraryDialog>
  );
}
