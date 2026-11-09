"use client";

import { useDroppable } from "@dnd-kit/core";
import {
  SortableContext,
  verticalListSortingStrategy,
} from "@dnd-kit/sortable";
import { Loader2 } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";

import { TableProgressBar } from "@/components/data-table/table-progress-bar";
import { Button } from "@/components/ui/button";
import { LIMITS } from "@/constants/limits";
import { DealCard } from "@/features/deals/components/deal-card";
import type {
  Deal,
  DealFilters,
  PipelineBoardStage,
} from "@/features/deals/types";
import { useStageDeals } from "@/features/deals/hooks/use-deals";
import { formatCurrency } from "@/lib/formatters";
import { cn } from "@/lib/utils";

const STAGE_DOT_CLASSES = [
  "bg-stage-1 border border-border/50",
  "bg-stage-2 border border-border/50",
  "bg-stage-3",
  "bg-stage-4",
  "bg-stage-5",
] as const;

interface Props {
  stage: PipelineBoardStage;
  filters?: Omit<DealFilters, "stageId" | "sort">;
  currency?: string;
  onReopen?: (deal: Deal) => void;
  optimisticDeals?: Deal[];
  settleDealId?: string | null;
}

export function DealsKanbanColumn({
  stage,
  filters,
  currency = LIMITS.defaultCurrency,
  onReopen,
  optimisticDeals,
  settleDealId,
}: Props) {
  const t = useTranslations("crm.deals.column");
  const locale = useLocale();

  const {
    deals: serverDeals,
    hasMore,
    loadMore,
    isLoadingMore,
    isFetching,
  } = useStageDeals(stage.id, filters);

  const deals = optimisticDeals ?? serverDeals;

  const { setNodeRef, isOver } = useDroppable({
    id: stage.id,
    data: {
      type: "column",
      stage,
    },
  });

  const colorIndex =
    typeof stage.colorIndex === "number"
      ? stage.colorIndex % STAGE_DOT_CLASSES.length
      : 0;
  const dotClass = STAGE_DOT_CLASSES[colorIndex] ?? STAGE_DOT_CLASSES[0];

  const totalAmountNum =
    typeof stage.totalAmount === "number"
      ? stage.totalAmount
      : parseFloat(stage.totalAmount || "0");

  const formattedTotal = formatCurrency(totalAmountNum, currency, locale);

  const dealIds = deals.map((d) => d.id);

  return (
    <div
      ref={setNodeRef}
      data-stage-kind={stage.kind}
      className={cn(
        "flex h-full w-72 shrink-0 flex-col rounded-[var(--radius-card)] border border-border/70 bg-card/60 transition-colors",
        isOver && "border-ring bg-muted/40",
      )}
    >
      {/* Column Header */}
      <div className="p-3 border-b border-border/60">
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-2 truncate">
            <span
              className={cn("h-2.5 w-2.5 shrink-0 rounded-full", dotClass)}
              aria-hidden="true"
            />
            <h3 className="text-xs font-semibold text-foreground truncate">
              {stage.name}
            </h3>
          </div>
          <span className="shrink-0 rounded-xs bg-muted px-1.5 py-0.5 text-[11px] font-medium tabular-nums text-muted-foreground">
            {stage.dealCount}
          </span>
        </div>
        <div className="mt-1 flex items-center justify-between text-[11px] text-muted-foreground tabular-nums">
          <span>{t("total")}</span>
          <span className="font-medium text-foreground">{formattedTotal}</span>
        </div>
      </div>

      <TableProgressBar active={isFetching && Boolean(deals.length)} />

      {/* Cards List */}
      <div className="flex-1 overflow-y-auto p-2 space-y-2 min-h-36">
        <SortableContext items={dealIds} strategy={verticalListSortingStrategy}>
          {deals.length === 0 ? (
            <div className="flex h-28 items-center justify-center rounded-[var(--radius-control)] border border-dashed border-border/60 p-3 text-center text-xs text-muted-foreground">
              {t("empty")}
            </div>
          ) : (
            deals.map((deal) => (
              <DealCard
                key={deal.id}
                deal={deal}
                onReopen={onReopen}
                settle={deal.id === settleDealId}
              />
            ))
          )}
        </SortableContext>

        {hasMore && (
          <div className="pt-1 pb-2 text-center">
            <Button
              type="button"
              variant="secondary"
              onClick={() => void loadMore()}
              disabled={isLoadingMore}
              className="h-7 w-full text-xs text-muted-foreground hover:text-foreground"
            >
              {isLoadingMore ? (
                <Loader2 className="mr-1 h-3 w-3 animate-spin" />
              ) : null}
              {t("showMore")}
            </Button>
          </div>
        )}
      </div>
    </div>
  );
}
