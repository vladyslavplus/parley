"use client";

import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import { Building2, Clock, GripVertical, RefreshCw, User } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { dealRecordRoute } from "@/constants/routes";
import type { Deal } from "@/features/deals/types";
import { Link } from "@/i18n/navigation";
import { formatCurrency, formatRelativeAge } from "@/lib/formatters";
import { cn } from "@/lib/utils";

interface Props {
  deal: Deal;
  isOverlay?: boolean;
  onReopen?: (deal: Deal) => void;
  settle?: boolean;
}

export function DealCard({
  deal,
  isOverlay = false,
  onReopen,
  settle = false,
}: Props) {
  const t = useTranslations("crm.deals.card");
  const tCrm = useTranslations("crm.contacts");
  const locale = useLocale();

  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({
    id: deal.id,
    data: {
      type: "deal",
      deal,
    },
    disabled: isOverlay,
  });

  const style = {
    transform: CSS.Translate.toString(transform),
    transition,
  };

  const amountDisplay = formatCurrency(
    typeof deal.amount === "number"
      ? deal.amount
      : parseFloat(deal.amount || "0"),
    deal.currency || LIMITS.defaultCurrency,
    locale,
  );

  const ownerName =
    deal.ownerDisplayName ||
    deal.ownerEmail ||
    tCrm("record.fields.unassigned");

  const isClosed = deal.status === "won" || deal.status === "lost";

  return (
    <div
      ref={setNodeRef}
      style={style}
      className={cn(
        "group relative rounded-[var(--radius-card)] border border-border bg-card p-3 text-left shadow-xs transition-shadow select-none",
        isDragging && "opacity-30 border-dashed border-ring",
        isOverlay &&
          "shadow-[var(--shadow-floating)] rotate-1 border-ring bg-card scale-102",
        !isDragging && !isOverlay && "hover:border-ring/60",
        settle && !isDragging && "animate-kanban-settle",
      )}
    >
      <div className="flex items-start justify-between gap-1.5">
        <div className="flex items-start gap-1 flex-1 min-w-0">
          {!isOverlay && (
            <button
              type="button"
              {...attributes}
              {...listeners}
              aria-label={t("dragHandle", { title: deal.title })}
              className="mt-0.5 -ml-1 flex h-4 w-4 shrink-0 items-center justify-center rounded-xs text-muted-foreground/60 hover:text-foreground hover:bg-muted cursor-grab active:cursor-grabbing focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring transition-colors"
            >
              <GripVertical className="h-3 w-3" />
            </button>
          )}
          <Link
            href={dealRecordRoute(deal.id)}
            className="text-xs font-semibold text-foreground hover:text-ring hover:underline line-clamp-2 leading-tight flex-1"
          >
            {deal.title}
          </Link>
        </div>
        {isClosed && (
          <span
            className={cn(
              "shrink-0 rounded-xs px-1.5 py-0.5 text-[10px] font-medium uppercase tracking-wider",
              deal.status === "won"
                ? "bg-success/15 text-success"
                : "bg-destructive/15 text-destructive",
            )}
          >
            {deal.status === "won" ? t("won") : t("lost")}
          </span>
        )}
      </div>

      <div className="mt-2 text-sm font-semibold tabular-nums text-foreground">
        {amountDisplay}
      </div>

      <div className="mt-2 space-y-1 text-[11px] text-muted-foreground">
        {deal.companyName && (
          <div className="flex items-center gap-1.5 truncate">
            <Building2 className="h-3 w-3 shrink-0" />
            <span className="truncate">{deal.companyName}</span>
          </div>
        )}
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-1.5 truncate">
            <User className="h-3 w-3 shrink-0" />
            <span className="truncate">{ownerName}</span>
          </div>
          <div className="flex items-center gap-1 shrink-0 text-[10px]">
            <Clock className="h-2.5 w-2.5 shrink-0" />
            <span>{formatRelativeAge(deal.createdAt, locale)}</span>
          </div>
        </div>
      </div>

      {isClosed && onReopen && (
        <div className="mt-2.5 pt-2 border-t border-border flex justify-end">
          <button
            type="button"
            onClick={(e) => {
              e.stopPropagation();
              onReopen(deal);
            }}
            className="flex items-center gap-1 text-[11px] font-medium text-ring hover:underline focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          >
            <RefreshCw className="h-3 w-3" />
            <span>{t("reopen")}</span>
          </button>
        </div>
      )}
    </div>
  );
}
