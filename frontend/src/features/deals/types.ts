import type { Schemas } from "@/lib/api/schemas";

export type Deal = Schemas["DealView"];
export type DealPage = Schemas["KeysetCursorPageOfDealView"];
export type Pipeline = Schemas["PipelineView"];
export type PipelineSummary = Schemas["PipelineSummaryView"];
export type PipelineStage = Schemas["PipelineStageView"];
export type PipelineBoard = Schemas["PipelineBoardView"];
export type PipelineBoardStage = Schemas["PipelineBoardStageView"];

export type StageKind = "open" | "won" | "lost";

export interface DealFilters {
  q?: string;
  stageId?: string;
  ownerUserId?: string;
  companyId?: string;
  contactId?: string;
  status?: "open" | "won" | "lost";
  sort?: "name" | "amount" | "created" | "position";
  order?: "asc" | "desc";
  limit?: number;
  after?: string | null;
}
