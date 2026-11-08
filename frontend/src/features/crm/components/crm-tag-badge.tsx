"use client";

import { X } from "lucide-react";
import { useTranslations } from "next-intl";
import { cn } from "@/lib/utils";

const TAG_COLOR_CLASSES = [
  "bg-tag-1 text-tag-1-foreground border-transparent",
  "bg-tag-2 text-tag-2-foreground border-transparent",
  "bg-tag-3 text-tag-3-foreground border-transparent",
  "bg-tag-4 text-tag-4-foreground border-transparent",
  "bg-tag-5 text-tag-5-foreground border-transparent",
] as const;

interface Props {
  name: string;
  colorIndex: number | string;
  onRemove?: () => void;
  className?: string;
  removeAriaLabel?: string;
}

export function CrmTagBadge({
  name,
  colorIndex,
  onRemove,
  className,
  removeAriaLabel,
}: Props) {
  const t = useTranslations("crm.tags");
  const index = Math.abs(Number(colorIndex) || 0) % TAG_COLOR_CLASSES.length;
  const colorClass = TAG_COLOR_CLASSES[index];

  return (
    <span
      className={cn(
        "inline-flex items-center gap-1 rounded-[var(--radius-control)] border px-2 py-0.5 text-xs font-medium transition-colors",
        colorClass,
        className,
      )}
    >
      <span>{name}</span>
      {onRemove && (
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            onRemove();
          }}
          className="ml-0.5 rounded p-0.5 hover:bg-foreground/10 transition-colors cursor-pointer"
          aria-label={removeAriaLabel ?? t("detach")}
        >
          <X className="h-3 w-3" />
        </button>
      )}
    </span>
  );
}
