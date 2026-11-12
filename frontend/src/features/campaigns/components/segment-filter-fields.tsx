"use client";

import { Lock, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { useWatch, type UseFormReturn } from "react-hook-form";

import { FormField } from "@/components/forms/form-field";
import { Input } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import { OwnerPicker } from "@/features/crm/components/owner-picker";
import { useOwners } from "@/features/crm/hooks/use-owners";
import { useTags } from "@/features/crm/hooks/use-tags";
import { normalizeCountryCode } from "@/features/campaigns/lib/segment-definition";
import type { SegmentFormValues } from "@/features/campaigns/schemas/segment-schema";
import { cn } from "@/lib/utils";

interface Props {
  form: UseFormReturn<SegmentFormValues>;
  // RFM names an existing definition carries; read-only because RFM filters
  // cannot be edited or used until scoring has run.
  rfmSegments: readonly string[];
}

function RemovableChip({
  label,
  removeLabel,
  onRemove,
}: {
  label: string;
  removeLabel: string;
  onRemove: () => void;
}) {
  return (
    <span className="inline-flex h-7 max-w-full items-center gap-1 rounded-full border border-border bg-muted pl-2.5 pr-1 text-xs text-foreground">
      <span className="min-w-0 truncate">{label}</span>
      <button
        type="button"
        onClick={onRemove}
        aria-label={removeLabel}
        className="flex h-5 w-5 shrink-0 cursor-pointer items-center justify-center rounded-full text-muted-foreground transition-colors hover:bg-background hover:text-foreground focus-visible:outline-2 focus-visible:outline-ring"
      >
        <X className="h-3 w-3" />
      </button>
    </span>
  );
}

function OwnerChips({
  ids,
  onRemove,
}: {
  ids: string[];
  onRemove: (id: string) => void;
}) {
  const t = useTranslations("campaigns.audience.fields");
  const owners = useOwners(
    { limit: LIMITS.ownerPickerMaxLimit },
    { enabled: ids.length > 0 },
  );
  return (
    <>
      {ids.map((id) => {
        const owner = owners.data?.items.find((item) => item.id === id);
        const label = owner?.displayName || owner?.email || t("ownerFallback");
        return (
          <RemovableChip
            key={id}
            label={label}
            removeLabel={t("removeValue", { value: label })}
            onRemove={() => onRemove(id)}
          />
        );
      })}
    </>
  );
}

export function SegmentFilterFields({ form, rfmSegments }: Props) {
  const t = useTranslations("campaigns.audience.fields");
  const [countryInput, setCountryInput] = useState("");
  const [countryInvalid, setCountryInvalid] = useState(false);

  const owners = useWatch({ control: form.control, name: "ownerUserIds" });
  const countries = useWatch({ control: form.control, name: "countryCodes" });
  const tagIds = useWatch({ control: form.control, name: "tagIds" });
  const tags = useTags({ limit: LIMITS.crmLookupLimit });
  const tagItems = tags.data?.items ?? [];
  const errors = form.formState.errors;

  function addCountry() {
    if (countryInput.trim() === "") return;
    const code = normalizeCountryCode(countryInput);
    if (!code) {
      setCountryInvalid(true);
      return;
    }
    setCountryInvalid(false);
    setCountryInput("");
    if (!countries.includes(code)) {
      form.setValue("countryCodes", [...countries, code], {
        shouldDirty: true,
      });
    }
  }

  function toggleTag(id: string) {
    form.setValue(
      "tagIds",
      tagIds.includes(id) ? tagIds.filter((x) => x !== id) : [...tagIds, id],
      { shouldDirty: true },
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <FormField label={t("owners")} hint={t("ownersHint")}>
        <div className="flex flex-col gap-2">
          <OwnerPicker
            value={null}
            allowClear={false}
            placeholder={t("addOwner")}
            ariaLabel={t("addOwner")}
            onChange={(id) => {
              if (id && !owners.includes(id)) {
                form.setValue("ownerUserIds", [...owners, id], {
                  shouldDirty: true,
                });
              }
            }}
          />
          {owners.length > 0 ? (
            <div className="flex flex-wrap gap-1.5">
              <OwnerChips
                ids={owners}
                onRemove={(id) =>
                  form.setValue(
                    "ownerUserIds",
                    owners.filter((x) => x !== id),
                    { shouldDirty: true },
                  )
                }
              />
            </div>
          ) : null}
        </div>
      </FormField>

      <FormField
        label={t("countries")}
        hint={t("countriesHint")}
        error={countryInvalid ? t("countryInvalid") : undefined}
      >
        <div className="flex flex-col gap-2">
          <Input
            value={countryInput}
            maxLength={LIMITS.segmentCountryCodeLength}
            placeholder={t("countryPlaceholder")}
            onChange={(event) => {
              setCountryInput(event.target.value);
              setCountryInvalid(false);
            }}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                event.preventDefault();
                addCountry();
              }
            }}
            onBlur={addCountry}
            className="font-mono uppercase"
          />
          {countries.length > 0 ? (
            <div className="flex flex-wrap gap-1.5">
              {countries.map((code) => (
                <RemovableChip
                  key={code}
                  label={code}
                  removeLabel={t("removeValue", { value: code })}
                  onRemove={() =>
                    form.setValue(
                      "countryCodes",
                      countries.filter((x) => x !== code),
                      { shouldDirty: true },
                    )
                  }
                />
              ))}
            </div>
          ) : null}
        </div>
      </FormField>

      <FormField label={t("tags")} hint={t("tagsHint")}>
        <div className="flex flex-wrap gap-1.5">
          {tagItems.length === 0 ? (
            <span className="text-xs text-muted-foreground">{t("noTags")}</span>
          ) : (
            tagItems.map((tag) => {
              const selected = tagIds.includes(tag.id);
              return (
                <button
                  key={tag.id}
                  type="button"
                  aria-pressed={selected}
                  onClick={() => toggleTag(tag.id)}
                  className={cn(
                    "h-7 cursor-pointer rounded-full border px-2.5 text-xs transition-colors focus-visible:outline-2 focus-visible:outline-ring",
                    selected
                      ? "border-primary bg-primary text-primary-foreground"
                      : "border-border bg-muted text-foreground hover:bg-background",
                  )}
                >
                  {tag.name}
                </button>
              );
            })
          )}
        </div>
      </FormField>

      <FormField
        label={t("daysSinceLastOrder")}
        hint={t("daysSinceLastOrderHint")}
        error={errors.daysSinceLastOrder?.message}
      >
        <Input
          type="number"
          inputMode="numeric"
          min={1}
          max={LIMITS.segmentDaysSinceLastOrderMax}
          placeholder={t("daysPlaceholder")}
          {...form.register("daysSinceLastOrder")}
        />
      </FormField>

      <div
        role="status"
        className="flex items-start gap-3 rounded-[var(--radius-control)] border border-dashed border-border bg-background px-3 py-2.5"
      >
        <Lock className="mt-0.5 h-3.5 w-3.5 shrink-0 text-muted-foreground" />
        <div className="space-y-0.5 text-xs">
          <p className="font-medium text-foreground">{t("rfm.title")}</p>
          <p className="text-muted-foreground">{t("rfm.unavailable")}</p>
          {rfmSegments.length > 0 ? (
            <p className="font-mono text-[11px] text-muted-foreground">
              {rfmSegments.join(", ")}
            </p>
          ) : null}
        </div>
      </div>
    </div>
  );
}
