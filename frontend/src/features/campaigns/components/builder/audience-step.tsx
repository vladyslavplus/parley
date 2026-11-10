"use client";

import { Plus, Users } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import { PERMISSIONS } from "@/constants/permissions";
import { SegmentEditorDialog } from "@/features/campaigns/components/segment-editor-dialog";
import { useAudienceCount } from "@/features/campaigns/hooks/use-audience-count";
import { useErrorText } from "@/features/campaigns/hooks/use-error-text";
import { useSegments } from "@/features/campaigns/hooks/use-library";
import type { CampaignDraft } from "@/features/campaigns/lib/campaign-draft";
import { useCan } from "@/features/session/components/can";
import { cn } from "@/lib/utils";

interface Props {
  draft: CampaignDraft;
  onChange: (patch: Partial<CampaignDraft>) => void;
}

function Option({
  selected,
  label,
  onSelect,
}: {
  selected: boolean;
  label: string;
  onSelect: () => void;
}) {
  return (
    <button
      type="button"
      role="radio"
      aria-checked={selected}
      onClick={onSelect}
      className={cn(
        "flex w-full cursor-pointer items-center justify-between gap-3 rounded-[var(--radius-control)] border px-3 py-2 text-left text-sm font-medium text-foreground transition-colors focus-visible:outline-2 focus-visible:outline-ring",
        selected
          ? "border-ring bg-muted"
          : "border-border bg-card hover:bg-muted/60",
      )}
    >
      <span className="truncate">{label}</span>
    </button>
  );
}

export function AudienceStep({ draft, onChange }: Props) {
  const t = useTranslations("campaigns.builder.audience");
  const tCount = useTranslations("campaigns.audience");
  const tSegments = useTranslations("campaigns.segments");
  const locale = useLocale();
  const errorText = useErrorText();
  const canManage = useCan(PERMISSIONS.campaignsManage);
  const [search, setSearch] = useState("");
  const [editorOpen, setEditorOpen] = useState(false);

  const segments = useSegments({
    q:
      search.trim().length >= LIMITS.searchMinQueryLength
        ? search.trim()
        : undefined,
    limit: LIMITS.crmLookupLimit,
  });
  const items = segments.data?.items ?? [];
  const audience = useAudienceCount(draft.segmentId);

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
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
              onClick={() => setEditorOpen(true)}
              className="gap-1.5 px-3 text-xs"
            >
              <Plus className="h-4 w-4" />
              <span>{tSegments("new")}</span>
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

        {segments.error ? (
          <Alert tone="error">{errorText(segments.error)}</Alert>
        ) : (
          <div
            role="radiogroup"
            aria-label={t("choose")}
            className="max-h-72 space-y-1.5 overflow-y-auto pr-1"
          >
            <Option
              selected={draft.segmentId === null}
              label={t("everyone")}
              onSelect={() => onChange({ segmentId: null })}
            />
            {items.map((segment) => (
              <Option
                key={segment.id}
                selected={draft.segmentId === segment.id}
                label={segment.name}
                onSelect={() => onChange({ segmentId: segment.id })}
              />
            ))}
          </div>
        )}
        {!segments.isPending && items.length === 0 ? (
          <p className="text-xs text-muted-foreground">
            {tSegments("empty.description")}
          </p>
        ) : null}
      </div>

      <div className="space-y-3">
        <div
          role="status"
          aria-live="polite"
          className="flex min-h-14 items-center gap-3 rounded-[var(--radius-card)] border border-border bg-background px-4 py-3"
        >
          <Users className="h-4 w-4 shrink-0 text-muted-foreground" />
          {audience.error ? (
            <p className="text-xs text-destructive">
              {errorText(audience.error)}
            </p>
          ) : audience.count === null ? (
            <span
              aria-busy="true"
              className="h-5 w-40 animate-pulse rounded bg-muted motion-reduce:animate-none"
            />
          ) : (
            <p className="text-sm text-foreground">
              <span className="font-semibold tabular-nums">
                {new Intl.NumberFormat(locale).format(audience.count)}
              </span>{" "}
              <span className="text-muted-foreground">
                {tCount("countLabel", { count: audience.count })}
              </span>
            </p>
          )}
        </div>
        <p className="rounded-[var(--radius-card)] border border-dashed border-border bg-card/40 p-3 text-xs text-muted-foreground">
          {t("rfmUnavailable")}
        </p>
      </div>

      <SegmentEditorDialog
        open={editorOpen}
        onOpenChange={setEditorOpen}
        segmentId={null}
        onSaved={(saved) => onChange({ segmentId: saved.id })}
      />
    </div>
  );
}
