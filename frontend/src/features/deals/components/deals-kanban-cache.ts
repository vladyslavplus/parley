import type { InfiniteData, QueryClient } from "@tanstack/react-query";

import { queryKeys } from "@/constants/query-keys";
import { stageDealsQueryKey } from "@/features/deals/hooks/use-deals";
import type {
  Deal,
  DealFilters,
  DealPage,
  PipelineBoard,
  PipelineBoardStage,
} from "@/features/deals/types";

export interface MoveArgs {
  deal: Deal;
  targetStage: PipelineBoardStage;
  position: number;
  lostReason?: string | null;
}

export interface MoveSnapshot {
  previousSource: InfiniteData<DealPage> | undefined;
  previousTarget: InfiniteData<DealPage> | undefined;
  previousBoard: PipelineBoard | undefined;
  sourceKey: ReturnType<typeof stageDealsQueryKey>;
  targetKey: ReturnType<typeof stageDealsQueryKey>;
  boardKey: ReturnType<typeof queryKeys.pipelineBoard>;
}

// Writes the move into the query cache synchronously so the card lands in
// the target column in the same frame as the drop (no snap-back to origin).
export function applyOptimisticMove(
  queryClient: QueryClient,
  boardPipelineId: string,
  filters: Omit<DealFilters, "stageId" | "sort"> | undefined,
  { deal, targetStage, position, lostReason }: MoveArgs,
): MoveSnapshot {
  const sourceStageId = deal.stageId;
  const targetStageId = targetStage.id;
  const sourceKey = stageDealsQueryKey(sourceStageId, filters);
  const targetKey = stageDealsQueryKey(targetStageId, filters);
  const boardKey = queryKeys.pipelineBoard(boardPipelineId);

  void queryClient.cancelQueries({ queryKey: sourceKey });
  void queryClient.cancelQueries({ queryKey: targetKey });
  void queryClient.cancelQueries({ queryKey: boardKey });

  const previousSource =
    queryClient.getQueryData<InfiniteData<DealPage>>(sourceKey);
  const previousTarget =
    queryClient.getQueryData<InfiniteData<DealPage>>(targetKey);
  const previousBoard = queryClient.getQueryData<PipelineBoard>(boardKey);

  const updatedDeal: Deal = {
    ...deal,
    stageId: targetStageId,
    status: targetStage.kind,
    lostReason: targetStage.kind === "lost" ? (lostReason ?? null) : null,
    closedAt:
      targetStage.kind === "won" || targetStage.kind === "lost"
        ? new Date().toISOString()
        : null,
  };

  if (sourceStageId === targetStageId) {
    if (previousSource) {
      queryClient.setQueryData<InfiniteData<DealPage>>(sourceKey, (old) => {
        if (!old) return old;
        const items = old.pages
          .flatMap((p) => p.items)
          .filter((d) => d.id !== deal.id);
        const insertIdx = Math.max(0, Math.min(position, items.length));
        items.splice(insertIdx, 0, updatedDeal);
        let offset = 0;
        return {
          ...old,
          pages: old.pages.map((p) => {
            const count = p.items.length;
            const pageItems = items.slice(offset, offset + count);
            offset += count;
            return { ...p, items: pageItems };
          }),
        };
      });
    }
  } else {
    if (previousSource) {
      queryClient.setQueryData<InfiniteData<DealPage>>(sourceKey, (old) => {
        if (!old) return old;
        return {
          ...old,
          pages: old.pages.map((p) => ({
            ...p,
            items: p.items.filter((d) => d.id !== deal.id),
          })),
        };
      });
    }
    queryClient.setQueryData<InfiniteData<DealPage>>(targetKey, (old) => {
      if (!old) {
        return {
          pages: [{ items: [updatedDeal], nextCursor: null }],
          pageParams: [undefined],
        };
      }
      const items = old.pages
        .flatMap((p) => p.items)
        .filter((d) => d.id !== deal.id);
      const insertIdx = Math.max(0, Math.min(position, items.length));
      items.splice(insertIdx, 0, updatedDeal);
      let offset = 0;
      return {
        ...old,
        pages: old.pages.map((p, idx) => {
          const count = idx === 0 ? p.items.length + 1 : p.items.length;
          const pageItems = items.slice(offset, offset + count);
          offset += count;
          return { ...p, items: pageItems };
        }),
      };
    });
    if (previousBoard) {
      queryClient.setQueryData<PipelineBoard>(boardKey, (old) => {
        if (!old) return old;
        const dealAmount =
          typeof deal.amount === "number"
            ? deal.amount
            : parseFloat(String(deal.amount || "0"));
        return {
          ...old,
          stages: old.stages.map((st) => {
            const count = Number(st.dealCount || 0);
            const total = Number(st.totalAmount || 0);
            if (st.id === sourceStageId) {
              return {
                ...st,
                dealCount: Math.max(0, count - 1),
                totalAmount: Math.max(0, total - dealAmount),
              };
            }
            if (st.id === targetStageId) {
              return {
                ...st,
                dealCount: count + 1,
                totalAmount: total + dealAmount,
              };
            }
            return st;
          }),
        };
      });
    }
  }

  return {
    previousSource,
    previousTarget,
    previousBoard,
    sourceKey,
    targetKey,
    boardKey,
  };
}
