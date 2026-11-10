"use client";

import { Pencil, Plus } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";

import { FormField } from "@/components/forms/form-field";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PERMISSIONS } from "@/constants/permissions";
import { LIMITS } from "@/constants/limits";
import { TemplateEditorDialog } from "@/features/campaigns/components/template-editor-dialog";
import { TemplatePreviewFrame } from "@/features/campaigns/components/template-preview-frame";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import {
  useTemplate,
  useTemplatePreview,
  useTemplates,
} from "@/features/campaigns/hooks/use-library";
import type { CampaignDraft } from "@/features/campaigns/lib/campaign-draft";
import { useCan } from "@/features/session/components/can";
import { cn } from "@/lib/utils";

interface Props {
  draft: CampaignDraft;
  onChange: (patch: Partial<CampaignDraft>) => void;
}

function SelectedPreview({ templateId }: { templateId: string }) {
  const t = useTranslations("campaigns.builder.template");
  const errorText = useErrorText();
  const template = useTemplate(templateId);
  const preview = useTemplatePreview(templateId, template.data?.version);

  if (template.error || preview.error) {
    return (
      <Alert tone="error">{errorText(template.error ?? preview.error)}</Alert>
    );
  }
  if (!preview.data) {
    return (
      <div
        aria-busy="true"
        className="h-72 animate-pulse rounded-[var(--radius-card)] bg-muted motion-reduce:animate-none"
      />
    );
  }
  return (
    <section
      aria-label={t("previewTitle")}
      className="space-y-2 rounded-[var(--radius-card)] border border-border bg-background p-4"
    >
      <h3 className="text-sm font-semibold text-foreground">
        {t("previewTitle")}
      </h3>
      <TemplatePreviewFrame
        subject={preview.data.subject}
        body={preview.data.body}
      />
    </section>
  );
}

export function TemplateStep({ draft, onChange }: Props) {
  const t = useTranslations("campaigns.builder.template");
  const tLibrary = useTranslations("campaigns.templates");
  const canManage = useCan(PERMISSIONS.campaignsManage);
  const [search, setSearch] = useState("");
  const [editorOpen, setEditorOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const errorText = useErrorText();

  const templates = useTemplates({
    q:
      search.trim().length >= LIMITS.searchMinQueryLength
        ? search.trim()
        : undefined,
    limit: LIMITS.crmLookupLimit,
  });
  const items = templates.data?.items ?? [];

  function openEditor(id: string | null) {
    setEditingId(id);
    setEditorOpen(true);
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
      <div className="flex flex-col gap-5">
        <FormField label={t("name")} hint={t("nameHint")} required>
          <Input
            value={draft.name}
            maxLength={LIMITS.campaignNameMaxLength}
            placeholder={t("namePlaceholder")}
            onChange={(event) => onChange({ name: event.target.value })}
            autoFocus
          />
        </FormField>

        <div className="space-y-2">
          <div className="flex items-end justify-between gap-3">
            <div>
              <h2 className="text-sm font-semibold text-foreground">
                {t("choose")}
              </h2>
              <p className="text-xs text-muted-foreground">{t("chooseHint")}</p>
            </div>
            {canManage ? (
              <Button
                type="button"
                variant="secondary"
                onClick={() => openEditor(null)}
                className="gap-1.5 px-3 text-xs"
              >
                <Plus className="h-4 w-4" />
                <span>{tLibrary("new")}</span>
              </Button>
            ) : null}
          </div>

          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder={t("search")}
            aria-label={t("search")}
            className="text-xs"
          />

          {templates.error ? (
            <Alert tone="error">{errorText(templates.error)}</Alert>
          ) : items.length === 0 && !templates.isPending ? (
            <div className="rounded-[var(--radius-card)] border border-dashed border-border bg-card/40 p-6 text-center">
              <p className="text-sm font-medium text-foreground">
                {tLibrary("empty.title")}
              </p>
              <p className="mt-1 text-xs text-muted-foreground">
                {tLibrary("empty.description")}
              </p>
            </div>
          ) : (
            <div
              role="radiogroup"
              aria-label={t("choose")}
              className="max-h-72 space-y-1.5 overflow-y-auto pr-1"
            >
              {items.map((template) => {
                const selected = template.id === draft.templateId;
                return (
                  <div
                    key={template.id}
                    className={cn(
                      "flex items-center gap-2 rounded-[var(--radius-control)] border px-3 py-2 transition-colors",
                      selected
                        ? "border-ring bg-muted"
                        : "border-border bg-card hover:bg-muted/60",
                    )}
                  >
                    <button
                      type="button"
                      role="radio"
                      aria-checked={selected}
                      onClick={() => onChange({ templateId: template.id })}
                      className="flex min-w-0 flex-1 cursor-pointer items-center justify-between gap-3 text-left focus-visible:outline-2 focus-visible:outline-ring"
                    >
                      <span className="truncate text-sm font-medium text-foreground">
                        {template.name}
                      </span>
                      <span className="shrink-0 rounded-xs bg-background px-1.5 py-0.5 font-mono text-[10px] uppercase text-muted-foreground">
                        {template.locale}
                      </span>
                    </button>
                    {canManage ? (
                      <button
                        type="button"
                        onClick={() => openEditor(template.id)}
                        aria-label={`${tLibrary("edit")}: ${template.name}`}
                        title={tLibrary("edit")}
                        className="flex h-6 w-6 shrink-0 cursor-pointer items-center justify-center rounded-xs text-muted-foreground hover:bg-background hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
                      >
                        <Pencil className="h-3.5 w-3.5" />
                      </button>
                    ) : null}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>

      <div>
        {draft.templateId ? (
          <SelectedPreview templateId={draft.templateId} />
        ) : (
          <div className="flex h-72 items-center justify-center rounded-[var(--radius-card)] border border-dashed border-border bg-card/40 p-6 text-center text-xs text-muted-foreground">
            {t("noSelection")}
          </div>
        )}
      </div>

      <TemplateEditorDialog
        open={editorOpen}
        onOpenChange={setEditorOpen}
        templateId={editingId}
        onSaved={(saved) => onChange({ templateId: saved.id })}
      />
    </div>
  );
}
