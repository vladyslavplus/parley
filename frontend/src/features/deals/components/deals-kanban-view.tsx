"use client";

import { Plus } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { PERMISSIONS } from "@/constants/permissions";
import { Can } from "@/features/session/components/can";
import { dealRecordRoute } from "@/constants/routes";
import { DealsKanbanBoard } from "@/features/deals/components/deals-kanban-board";
import { DealsToolbar } from "@/features/deals/components/deals-toolbar";
import { DealQuickCreateDialog } from "@/features/deals/components/deal-quick-create-dialog";
import { useDealFilters } from "@/features/deals/hooks/use-deal-filters";
import { useDefaultPipeline } from "@/features/deals/hooks/use-pipelines";
import { useOrganizationCurrency } from "@/features/deals/hooks/use-organization-currency";
import { useRouter } from "@/i18n/navigation";
export function DealsKanbanView() {
  const t = useTranslations("crm.deals");
  const tToolbar = useTranslations("crm.deals.toolbar");
  const router = useRouter();
  const { board, isLoading } = useDefaultPipeline();
  const [quickCreateOpen, setQuickCreateOpen] = useState(false);

  const filters = useDealFilters();
  const organizationCurrency = useOrganizationCurrency();

  const boardFilters = {
    q: filters.q,
    ownerUserId: filters.ownerUserId,
    companyId: filters.companyId,
    status: filters.status === "all" ? undefined : filters.status,
  };

  return (
    <div className="space-y-4">
      {/* Page Header */}
      <div className="flex flex-col justify-between gap-4 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">
            {t("title")}
          </h1>
          <p className="mt-0.5 text-xs text-muted-foreground">
            {t("subtitle")}
          </p>
        </div>
        <Can permission={PERMISSIONS.dealsWrite}>
          <Button
            type="button"
            onClick={() => setQuickCreateOpen(true)}
            className="gap-1.5 self-start px-3 text-xs sm:self-auto"
          >
            <Plus className="h-4 w-4" />
            <span>{tToolbar("newDeal")}</span>
          </Button>
        </Can>
      </div>

      {/* Toolbar */}
      <DealsToolbar
        searchInput={filters.searchInput}
        onSearchChange={filters.setSearchInput}
        ownerUserId={filters.ownerUserId}
        onOwnerChange={(id) => filters.setOwnerUserId(id ?? "")}
        companyId={filters.companyId}
        onCompanyChange={(id) => filters.setCompanyId(id ?? "")}
        status={filters.status}
        onStatusChange={filters.setStatus}
        hasActiveFilters={filters.hasActiveFilters}
        onClearFilters={filters.clearFilters}
      />

      {/* Board or Skeleton */}
      {isLoading && !board ? (
        <div className="flex h-[calc(100vh-14rem)] gap-4 overflow-hidden">
          {Array.from({ length: 4 }).map((_, idx) => (
            <div
              key={idx}
              className="h-full w-72 shrink-0 rounded-[var(--radius-card)] border border-border/70 bg-card/60 p-3 space-y-3"
            >
              <div className="flex items-center justify-between">
                <Skeleton className="h-4 w-28" />
                <Skeleton className="h-4 w-8" />
              </div>
              <Skeleton className="h-3 w-16" />
              <div className="space-y-2 pt-2">
                <Skeleton className="h-24 w-full rounded-[var(--radius-card)]" />
                <Skeleton className="h-24 w-full rounded-[var(--radius-card)]" />
                <Skeleton className="h-24 w-full rounded-[var(--radius-card)]" />
              </div>
            </div>
          ))}
        </div>
      ) : board ? (
        <DealsKanbanBoard
          board={board}
          filters={boardFilters}
          currency={organizationCurrency}
        />
      ) : (
        <div className="flex h-64 items-center justify-center rounded-[var(--radius-card)] border border-border bg-card p-8 text-center text-xs text-muted-foreground">
          {t("board.noPipeline")}
        </div>
      )}

      {/* Quick create modal */}
      <DealQuickCreateDialog
        open={quickCreateOpen}
        onOpenChange={setQuickCreateOpen}
        onCreated={(id) => router.push(dealRecordRoute(id))}
        defaultStageId={board?.stages.find((s) => s.kind === "open")?.id}
      />
    </div>
  );
}
