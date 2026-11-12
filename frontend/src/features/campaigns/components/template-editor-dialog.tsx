"use client";

import { Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";
import { useRef, useState } from "react";

import { FormField } from "@/components/forms/form-field";
import { FormSelect } from "@/components/forms/form-select";
import { useApiForm } from "@/components/forms/use-api-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input, Textarea } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import { LibraryDialog } from "@/features/campaigns/components/library-dialog";
import { TemplatePreviewFrame } from "@/features/campaigns/components/template-preview-frame";
import {
  useTemplate,
  useTemplatePreview,
} from "@/features/campaigns/hooks/use-library";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { useLibraryMutations } from "@/features/campaigns/hooks/use-library-mutations";
import {
  templateFormSchema,
  type TemplateFormValues,
} from "@/features/campaigns/schemas/template-schema";
import {
  TEMPLATE_FILTERS,
  TEMPLATE_LOCALES,
  TEMPLATE_TAGS,
  TEMPLATE_VARIABLES,
  type Template,
} from "@/features/campaigns/types";
import { ERROR_CODES } from "@/constants/error-codes";
import { toApiError } from "@/lib/errors";
import { showErrorToast, showSuccessToast } from "@/lib/toasts";
import { cn } from "@/lib/utils";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  // Existing template to edit; null or undefined creates a new one.
  templateId?: string | null;
  onSaved?: (template: Template) => void;
}

type EditableField = "subjectLiquid" | "bodyLiquid";

function EditorBody({
  initial,
  onClose,
  onSaved,
}: {
  initial: Template | null;
  onClose: () => void;
  onSaved?: (template: Template) => void;
}) {
  const t = useTranslations("campaigns.templates");
  const tCommon = useTranslations("common");
  const tValidation = useTranslations("validation");
  const tProfile = useTranslations("settings.profile");
  const errorText = useErrorText();
  const { createTemplate, updateTemplate } = useLibraryMutations();

  const [saved, setSaved] = useState<Template | null>(initial);
  const subjectRef = useRef<HTMLInputElement | null>(null);
  const bodyRef = useRef<HTMLTextAreaElement | null>(null);
  const [lastField, setLastField] = useState<EditableField>("bodyLiquid");

  const { form, onSubmit, formError, alertRef } =
    useApiForm<TemplateFormValues>({
      schema: templateFormSchema(tValidation),
      defaultValues: {
        name: initial?.name ?? "",
        locale: (initial?.locale as TemplateFormValues["locale"]) ?? "en",
        subjectLiquid: initial?.subjectLiquid ?? "",
        bodyLiquid: initial?.bodyLiquid ?? "",
      },
      fields: ["name", "locale", "subjectLiquid", "bodyLiquid"],
      submit: async (values) => {
        try {
          const result = saved
            ? await updateTemplate(saved.id, {
                ...values,
                version: saved.version,
              })
            : await createTemplate({ ...values, version: null });
          setSaved(result);
          form.reset(values);
          showSuccessToast(t(saved ? "updated" : "created"));
          onSaved?.(result);
        } catch (caught) {
          const error = toApiError(caught);
          if (error.code === ERROR_CODES.templateVersionConflict) {
            showErrorToast(errorText(error));
          }
          throw error;
        }
      },
    });

  const {
    register,
    formState: { errors, isSubmitting, isDirty },
  } = form;

  const preview = useTemplatePreview(saved?.id, saved?.version);

  const subjectField = register("subjectLiquid");
  const bodyField = register("bodyLiquid");

  function insertVariable(name: string) {
    const token = `{{ ${name} }}`;
    const target: EditableField = lastField;
    const element: HTMLInputElement | HTMLTextAreaElement | null =
      target === "subjectLiquid" ? subjectRef.current : bodyRef.current;
    const current = form.getValues(target);
    const start = element?.selectionStart ?? current.length;
    const end = element?.selectionEnd ?? current.length;
    form.setValue(
      target,
      current.slice(0, start) + token + current.slice(end),
      {
        shouldDirty: true,
        shouldValidate: true,
      },
    );
    element?.focus();
    const caret = start + token.length;
    requestAnimationFrame(() => element?.setSelectionRange(caret, caret));
  }

  const localeOptions = TEMPLATE_LOCALES.map((code) => ({
    value: code,
    label: tProfile(`locales.${code}`),
  }));

  return (
    <div className="grid gap-6 lg:grid-cols-2">
      <form onSubmit={onSubmit} noValidate className="flex flex-col gap-4">
        {formError ? (
          <Alert ref={alertRef} tone="error">
            {formError}
          </Alert>
        ) : null}

        <div className="grid gap-4 sm:grid-cols-[1fr_10rem]">
          <FormField
            label={t("form.name")}
            error={errors.name?.message}
            required
          >
            <Input
              {...register("name")}
              maxLength={LIMITS.templateNameMaxLength}
              placeholder={t("form.namePlaceholder")}
              autoFocus
            />
          </FormField>
          <FormField label={t("form.locale")} error={errors.locale?.message}>
            <FormSelect
              control={form.control}
              name="locale"
              options={localeOptions}
            />
          </FormField>
        </div>

        <FormField
          label={t("form.subject")}
          error={errors.subjectLiquid?.message}
          required
        >
          <Input
            {...subjectField}
            ref={(element) => {
              subjectField.ref(element);
              subjectRef.current = element;
            }}
            onFocus={() => setLastField("subjectLiquid")}
            maxLength={LIMITS.templateSubjectMaxLength}
            placeholder={t("form.subjectPlaceholder")}
          />
        </FormField>

        <div className="space-y-2">
          <FormField
            label={t("form.body")}
            error={errors.bodyLiquid?.message}
            required
          >
            <Textarea
              {...bodyField}
              ref={(element) => {
                bodyField.ref(element);
                bodyRef.current = element;
              }}
              onFocus={() => setLastField("bodyLiquid")}
              rows={12}
              spellCheck={false}
              maxLength={LIMITS.templateBodyMaxLength}
              className="font-mono text-xs"
            />
          </FormField>

          <div className="space-y-1.5">
            <p className="text-xs font-medium text-foreground">
              {t("variables.title")}
            </p>
            <div className="flex flex-wrap gap-1.5">
              {TEMPLATE_VARIABLES.map((name) => (
                <button
                  key={name}
                  type="button"
                  onClick={() => insertVariable(name)}
                  className="cursor-pointer rounded-full border border-border bg-muted px-2.5 py-1 font-mono text-[11px] text-foreground transition-colors hover:bg-background focus-visible:outline-2 focus-visible:outline-ring"
                >
                  {name}
                </button>
              ))}
            </div>
            <p className="text-[11px] text-muted-foreground">
              {t("variables.hint")}
            </p>
          </div>

          <details className="rounded-[var(--radius-control)] border border-border bg-background px-3 py-2 text-xs">
            <summary className="cursor-pointer font-medium text-foreground">
              {t("help.title")}
            </summary>
            <div className="mt-2 space-y-2 text-muted-foreground">
              <p>{t("help.intro")}</p>
              <p>
                <span className="font-medium text-foreground">
                  {t("help.tags")}:{" "}
                </span>
                <span className="font-mono">{TEMPLATE_TAGS.join(", ")}</span>
              </p>
              <p>
                <span className="font-medium text-foreground">
                  {t("help.filters")}:{" "}
                </span>
                <span className="font-mono">{TEMPLATE_FILTERS.join(", ")}</span>
              </p>
              <p>{t("help.noIncludes")}</p>
            </div>
          </details>
        </div>

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
            disabled={isSubmitting || (!!saved && !isDirty)}
          >
            {isSubmitting ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
            ) : null}
            {saved ? t("save") : t("create")}
          </Button>
        </div>
      </form>

      <section
        aria-label={t("preview.title")}
        className="flex min-w-0 flex-col gap-3 rounded-[var(--radius-card)] border border-border bg-background p-4"
      >
        <div className="space-y-1">
          <h3 className="text-sm font-semibold text-foreground">
            {t("preview.title")}
          </h3>
          <p className="text-xs text-muted-foreground">{t("preview.hint")}</p>
        </div>
        <PreviewPanel
          hasSaved={Boolean(saved)}
          isDirty={isDirty}
          isPending={preview.isFetching}
          error={preview.error}
          data={preview.data}
        />
      </section>
    </div>
  );
}

function PreviewPanel({
  hasSaved,
  isDirty,
  isPending,
  error,
  data,
}: {
  hasSaved: boolean;
  isDirty: boolean;
  isPending: boolean;
  error: unknown;
  data: { subject: string; body: string } | undefined;
}) {
  const t = useTranslations("campaigns.templates.preview");
  const errorText = useErrorText();

  if (!hasSaved) {
    return (
      <div className="flex h-96 items-center justify-center rounded-[var(--radius-control)] border border-dashed border-border p-6 text-center text-xs text-muted-foreground">
        {t("saveFirst")}
      </div>
    );
  }
  if (error) {
    return <Alert tone="error">{errorText(error)}</Alert>;
  }
  if (!data) {
    return (
      <div
        aria-busy="true"
        className="h-96 animate-pulse rounded-[var(--radius-control)] bg-muted motion-reduce:animate-none"
      />
    );
  }
  return (
    <div className={cn("space-y-2", isPending && "opacity-70")}>
      {isDirty ? <Alert tone="info">{t("unsaved")}</Alert> : null}
      <TemplatePreviewFrame subject={data.subject} body={data.body} />
    </div>
  );
}

function LoadedEditor({
  templateId,
  onClose,
  onSaved,
}: {
  templateId: string | null;
  onClose: () => void;
  onSaved?: (template: Template) => void;
}) {
  const query = useTemplate(templateId);
  const errorText = useErrorText();

  if (templateId && query.error) {
    return <Alert tone="error">{errorText(query.error)}</Alert>;
  }
  if (templateId && !query.data) {
    return (
      <div
        aria-busy="true"
        className="h-96 animate-pulse rounded-[var(--radius-card)] bg-muted motion-reduce:animate-none"
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

export function TemplateEditorDialog({
  open,
  onOpenChange,
  templateId = null,
  onSaved,
}: Props) {
  const t = useTranslations("campaigns.templates");
  return (
    <LibraryDialog
      open={open}
      onOpenChange={onOpenChange}
      size="xl"
      title={templateId ? t("editTitle") : t("newTitle")}
      description={t("dialogDescription")}
    >
      <LoadedEditor
        templateId={templateId}
        onClose={() => onOpenChange(false)}
        onSaved={onSaved}
      />
    </LibraryDialog>
  );
}
