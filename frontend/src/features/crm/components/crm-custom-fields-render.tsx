"use client";

import { useTranslations } from "next-intl";

import { InlineEdit } from "@/components/ui/inline-edit";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { CustomFieldDefinition } from "@/features/crm/types";
import { showErrorToast } from "@/lib/toasts";

interface Props {
  definitions: CustomFieldDefinition[];
  values: Record<string, unknown>;
  onSave: (key: string, value: unknown) => Promise<void> | void;
  disabled?: boolean;
}

export function CrmCustomFieldsRender({
  definitions,
  values,
  onSave,
  disabled = false,
}: Props) {
  const t = useTranslations("crm.customFields");

  // Show active defs + archived defs that already have values present on this entity
  const visibleDefs = definitions
    .filter((d) => {
      if (!d.archivedAt) return true;
      const val = values[d.key];
      return val !== undefined && val !== null && val !== "";
    })
    .sort((a, b) => Number(a.position) - Number(b.position));

  if (visibleDefs.length === 0) {
    return <p className="text-xs text-muted-foreground">{t("empty")}</p>;
  }

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
      {visibleDefs.map((def) => {
        const rawVal = values[def.key];
        const isArchived = Boolean(def.archivedAt);
        const isFieldDisabled = disabled || isArchived;

        return (
          <div
            key={def.id}
            className={`flex flex-col gap-1.5 rounded-[var(--radius-control)] border border-border p-3 transition-colors ${
              isArchived ? "bg-muted/30 text-muted-foreground" : "bg-card"
            }`}
          >
            <div className="flex items-center justify-between gap-2">
              <span className="text-xs font-medium text-muted-foreground">
                {def.label}
              </span>
              <div className="flex items-center gap-1.5">
                {isArchived && (
                  <span className="text-[10px] font-semibold uppercase tracking-wider text-muted-foreground bg-muted px-1.5 py-0.5 rounded">
                    {t("archived")}
                  </span>
                )}
                {def.isRequired && !isArchived && (
                  <span className="text-[10px] font-semibold text-warning">
                    {t("required")}
                  </span>
                )}
              </div>
            </div>

            {def.fieldType === "boolean" ? (
              <label className="flex items-center gap-2 cursor-pointer pt-1">
                <input
                  type="checkbox"
                  disabled={isFieldDisabled}
                  checked={Boolean(rawVal)}
                  onChange={(e) => void onSave(def.key, e.target.checked)}
                  className="h-4 w-4 rounded border-input text-primary focus:ring-ring disabled:cursor-not-allowed"
                />
                <span className="text-sm font-medium text-foreground">
                  {Boolean(rawVal) ? t("yes") : t("no")}
                </span>
              </label>
            ) : def.fieldType === "select" ? (
              isArchived || isFieldDisabled ? (
                <span className="text-xs font-medium text-foreground pt-1">
                  {typeof rawVal === "string" && rawVal ? rawVal : "—"}
                </span>
              ) : (
                <Select
                  value={typeof rawVal === "string" ? rawVal : ""}
                  onValueChange={(val) => void onSave(def.key, val || null)}
                >
                  <SelectTrigger className="text-xs bg-background">
                    <SelectValue placeholder={t("selectOption")} />
                  </SelectTrigger>
                  <SelectContent>
                    {def.options?.map((opt) => (
                      <SelectItem key={opt} value={opt}>
                        {opt}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )
            ) : def.fieldType === "date" ? (
              isArchived || isFieldDisabled ? (
                <span className="text-xs font-mono text-foreground pt-1">
                  {typeof rawVal === "string" && rawVal ? rawVal : "—"}
                </span>
              ) : (
                <Input
                  type="date"
                  disabled={isFieldDisabled}
                  value={typeof rawVal === "string" ? rawVal : ""}
                  onChange={(e) => void onSave(def.key, e.target.value || null)}
                  className="text-xs bg-background"
                />
              )
            ) : isArchived ? (
              <span className="text-xs text-foreground font-medium pt-1">
                {rawVal !== null && rawVal !== undefined ? String(rawVal) : "—"}
              </span>
            ) : (
              <InlineEdit
                disabled={isFieldDisabled}
                value={
                  rawVal !== null && rawVal !== undefined ? String(rawVal) : ""
                }
                onSave={(val) => {
                  if (def.fieldType === "number") {
                    if (val === "") {
                      return onSave(def.key, null);
                    }
                    const num = Number(val);
                    if (!Number.isFinite(num)) {
                      showErrorToast(t("invalidNumber"));
                      return;
                    }
                    return onSave(def.key, num);
                  }
                  return onSave(def.key, val || null);
                }}
                placeholder={def.label}
                ariaLabel={def.label}
              />
            )}
          </div>
        );
      })}
    </div>
  );
}
