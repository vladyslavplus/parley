"use client";

import {
  DndContext,
  DragOverlay,
  KeyboardSensor,
  PointerSensor,
  useSensor,
  useSensors,
  type DragEndEvent,
  type DragStartEvent,
} from "@dnd-kit/core";
import { sortableKeyboardCoordinates } from "@dnd-kit/sortable";
import {
  type InfiniteData,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { queryKeys } from "@/constants/query-keys";
import {
  applyOptimisticMove,
  type MoveArgs,
  type MoveSnapshot,
} from "@/features/deals/components/deals-kanban-cache";
import { DealsKanbanColumn } from "@/features/deals/components/deals-kanban-column";
import { DealCard } from "@/features/deals/components/deal-card";
import { DealWonLostDialog } from "@/features/deals/components/deal-won-lost-dialog";
import { stageDealsQueryKey } from "@/features/deals/hooks/use-deals";
import type {
  Deal,
  DealFilters,
  DealPage,
  PipelineBoard,
  PipelineBoardStage,
} from "@/features/deals/types";
import { backendJson } from "@/lib/api/bff-client";
import { describeError, toApiError } from "@/lib/errors";
import { showErrorToast } from "@/lib/toasts";

interface Props {
  board: PipelineBoard;
  filters?: Omit<DealFilters, "stageId" | "sort">;
  currency?: string;
}

interface PendingMove {
  deal: Deal;
  targetStage: PipelineBoardStage;
  position: number;
}

export function DealsKanbanBoard({ board, filters, currency }: Props) {
  const tAnnouncements = useTranslations("crm.deals.accessibility");
  const tErrors = useTranslations("errors");
  const locale = useLocale();
  const queryClient = useQueryClient();

  const [activeDeal, setActiveDeal] = useState<Deal | null>(null);
  const [terminalModalOpen, setTerminalModalOpen] = useState(false);
  const [pendingMove, setPendingMove] = useState<PendingMove | null>(null);
  const [settleDealId, setSettleDealId] = useState<string | null>(null);

  const sensors = useSensors(
    useSensor(PointerSensor, {
      activationConstraint: {
        distance: 6,
      },
    }),
    useSensor(KeyboardSensor, {
      coordinateGetter: sortableKeyboardCoordinates,
    }),
  );

  const moveMutation = useMutation({
    mutationFn: async ({
      deal,
      targetStage,
      position,
      lostReason,
    }: MoveArgs & { snapshot: MoveSnapshot }) => {
      return backendJson<Deal>(
        "/deals/{id}/move",
        {
          method: "POST",
          locale,
          body: {
            stageId: targetStage.id,
            position,
            version: deal.version,
            lostReason: lostReason ?? null,
          },
        },
        `/deals/${encodeURIComponent(deal.id)}/move`,
      );
    },
    onError: (err, { snapshot }) => {
      if (snapshot.previousSource) {
        queryClient.setQueryData(snapshot.sourceKey, snapshot.previousSource);
      }
      if (snapshot.previousTarget) {
        queryClient.setQueryData(snapshot.targetKey, snapshot.previousTarget);
      }
      if (snapshot.previousBoard) {
        queryClient.setQueryData(snapshot.boardKey, snapshot.previousBoard);
      }
      const apiErr = toApiError(err);
      showErrorToast(
        describeError(apiErr, (kind) =>
          tErrors(kind as Parameters<typeof tErrors>[0]),
        ),
      );
    },
    onSettled: (_data, _err, variables) => {
      const { snapshot } = variables;
      queryClient.invalidateQueries({ queryKey: snapshot.sourceKey });
      queryClient.invalidateQueries({ queryKey: snapshot.targetKey });
      queryClient.invalidateQueries({ queryKey: snapshot.boardKey });
      queryClient.invalidateQueries({
        queryKey: queryKeys.deal(variables.deal.id),
      });
      queryClient.invalidateQueries({ queryKey: queryKeys.dealLists() });
      queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() });
    },
  });

  function startMove(args: MoveArgs) {
    const snapshot = applyOptimisticMove(
      queryClient,
      board.pipelineId,
      filters,
      args,
    );
    setSettleDealId(args.deal.id);
    return moveMutation.mutateAsync({ ...args, snapshot }).catch(() => null);
  }

  function handleDragStart(event: DragStartEvent) {
    const deal = event.active.data.current?.deal as Deal | undefined;
    if (deal) {
      setActiveDeal(deal);
    }
  }

  function handleDragEnd(event: DragEndEvent) {
    const { active, over } = event;
    setActiveDeal(null);

    if (!over) return;

    const deal = active.data.current?.deal as Deal | undefined;
    if (!deal) return;

    let targetStage: PipelineBoardStage | undefined;
    let targetPosition = 0;

    const overType = over.data.current?.type;
    if (overType === "column") {
      targetStage = over.data.current?.stage as PipelineBoardStage;
      targetPosition = 0;
    } else if (overType === "deal") {
      const overDeal = over.data.current?.deal as Deal;
      targetStage = board.stages.find((s) => s.id === overDeal.stageId);
      if (targetStage) {
        const targetKey = stageDealsQueryKey(targetStage.id, filters);
        const targetPages =
          queryClient.getQueryData<InfiniteData<DealPage>>(targetKey);
        const targetDeals = targetPages?.pages.flatMap((p) => p.items) ?? [];
        const overIndex = targetDeals.findIndex((d) => d.id === overDeal.id);
        targetPosition = overIndex >= 0 ? overIndex : 0;
      }
    } else {
      targetStage = board.stages.find((s) => s.id === over.id);
      targetPosition = 0;
    }

    if (!targetStage) return;

    if (
      deal.stageId === targetStage.id &&
      overType === "deal" &&
      active.id === over.id
    ) {
      return;
    }

    if (
      (targetStage.kind === "won" || targetStage.kind === "lost") &&
      deal.stageId !== targetStage.id
    ) {
      setPendingMove({
        deal,
        targetStage,
        position: targetPosition,
      });
      setTerminalModalOpen(true);
      return;
    }

    void startMove({ deal, targetStage, position: targetPosition });
  }

  async function handleTerminalConfirm(payload: {
    dealId: string;
    stageId: string;
    position: number;
    version: number | string;
    lostReason?: string | null;
  }) {
    if (!pendingMove) return;
    await startMove({
      deal: pendingMove.deal,
      targetStage: pendingMove.targetStage,
      position: payload.position,
      lostReason: payload.lostReason,
    });
    setPendingMove(null);
  }

  function handleReopen(deal: Deal) {
    const firstOpenStage = board.stages.find((s) => s.kind === "open");
    if (!firstOpenStage) return;
    void startMove({ deal, targetStage: firstOpenStage, position: 0 });
  }

  return (
    <DndContext
      sensors={sensors}
      onDragStart={handleDragStart}
      onDragEnd={handleDragEnd}
      accessibility={{
        announcements: {
          onDragStart({ active }) {
            const deal = active.data.current?.deal as Deal | undefined;
            const title = deal?.title ?? "";
            return tAnnouncements("pickedUp", { title });
          },
          onDragOver({ active, over }) {
            const deal = active.data.current?.deal as Deal | undefined;
            const title = deal?.title ?? "";
            const targetName =
              (over?.data.current?.stage as PipelineBoardStage)?.name ??
              (over?.data.current?.deal as Deal)?.title ??
              "";
            return tAnnouncements("dragOver", { title, target: targetName });
          },
          onDragEnd({ active, over }) {
            const deal = active.data.current?.deal as Deal | undefined;
            const title = deal?.title ?? "";
            let stageName = "";
            if (over?.data.current?.type === "column") {
              stageName =
                (over.data.current.stage as PipelineBoardStage)?.name ?? "";
            } else if (over?.data.current?.type === "deal") {
              const overDeal = over.data.current.deal as Deal;
              stageName =
                board.stages.find((s) => s.id === overDeal.stageId)?.name ?? "";
            } else if (over) {
              stageName =
                board.stages.find((s) => s.id === over.id)?.name ?? "";
            }
            return tAnnouncements("dropped", { title, stage: stageName });
          },
          onDragCancel({ active }) {
            const deal = active.data.current?.deal as Deal | undefined;
            const title = deal?.title ?? "";
            return tAnnouncements("cancelled", { title });
          },
        },
      }}
    >
      <div
        data-kanban-board="true"
        className="flex h-[calc(100vh-14rem)] w-full items-start gap-4 overflow-x-auto pb-3"
      >
        {board.stages.map((stage) => (
          <DealsKanbanColumn
            key={stage.id}
            stage={stage}
            filters={filters}
            currency={currency}
            onReopen={handleReopen}
            settleDealId={settleDealId}
          />
        ))}
      </div>

      <DragOverlay dropAnimation={null}>
        {activeDeal ? <DealCard deal={activeDeal} isOverlay /> : null}
      </DragOverlay>

      <DealWonLostDialog
        open={terminalModalOpen}
        onOpenChange={(open) => {
          setTerminalModalOpen(open);
          if (!open) setPendingMove(null);
        }}
        deal={pendingMove?.deal ?? null}
        targetStage={pendingMove?.targetStage ?? null}
        position={pendingMove?.position ?? 0}
        onConfirm={handleTerminalConfirm}
      />
    </DndContext>
  );
}
