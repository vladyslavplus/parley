import type { DealStatusFilter } from "@/features/deals/hooks/use-deal-filters";

// An open stage only ever holds open deals, the won stage only won deals and
// the lost stage only lost deals, so a status filter leaves every other
// column empty. The board shows just the columns that can match.
export function stagesForStatus<T extends { kind: string }>(
  stages: readonly T[],
  status: DealStatusFilter,
): T[] {
  return status === "all"
    ? [...stages]
    : stages.filter((stage) => stage.kind === status);
}
