import { LIMITS } from "@/constants/limits";
import {
  TARGETING_MODES,
  toNumber,
  type Campaign,
  type TargetingMode,
} from "@/features/campaigns/types";
import type { Schemas } from "@/lib/api/schemas";

export const STEP_IDS = [
  "template",
  "audience",
  "experiment",
  "targeting",
  "review",
] as const;
export type StepId = (typeof STEP_IDS)[number];

export interface CampaignDraft {
  name: string;
  templateId: string | null;
  segmentId: string | null;
  holdoutShareBps: number;
  targetingMode: TargetingMode;
  targetingShareBps: number;
}

export function initialDraft(): CampaignDraft {
  return {
    name: "",
    templateId: null,
    segmentId: null,
    holdoutShareBps: LIMITS.holdoutShareDefaultBps,
    targetingMode: "all",
    targetingShareBps: LIMITS.targetingShareDefaultBps,
  };
}

export function draftFromCampaign(campaign: Campaign): CampaignDraft {
  const mode = TARGETING_MODES.find((m) => m === campaign.targetingMode);
  return {
    name: campaign.name,
    templateId: campaign.templateId,
    segmentId: campaign.segmentId,
    holdoutShareBps: toNumber(campaign.holdoutShareBps),
    targetingMode: mode ?? "all",
    targetingShareBps:
      campaign.targetingShareBps === null
        ? LIMITS.targetingShareDefaultBps
        : toNumber(campaign.targetingShareBps),
  };
}

export function isRankingMode(mode: TargetingMode): boolean {
  return mode !== "all";
}

// True once the user changed anything from a blank new draft.
export function isTouched(draft: CampaignDraft): boolean {
  return JSON.stringify(draft) !== JSON.stringify(initialDraft());
}

// A draft can be stored once it has the two things the server requires.
export function isSaveable(draft: CampaignDraft): boolean {
  return draft.name.trim().length > 0 && draft.templateId !== null;
}

export function draftToRequest(
  draft: CampaignDraft,
  version: number | null,
): Schemas["CampaignRequest"] {
  return {
    name: draft.name.trim(),
    templateId: draft.templateId,
    segmentId: draft.segmentId,
    holdoutShareBps: draft.holdoutShareBps,
    targetingMode: draft.targetingMode,
    targetingShareBps: isRankingMode(draft.targetingMode)
      ? draft.targetingShareBps
      : null,
    version,
  };
}

export interface StepContext {
  canRank: boolean;
}

// Gate for the "Next" button: a step may be left only when its own fields are
// valid. The server stays the source of truth and re-validates everything.
export function isStepComplete(
  step: StepId,
  draft: CampaignDraft,
  context: StepContext,
): boolean {
  switch (step) {
    case "template":
      return isSaveable(draft);
    case "audience":
      return true;
    case "experiment":
      return (
        draft.holdoutShareBps >= LIMITS.holdoutShareMinBps &&
        draft.holdoutShareBps <= LIMITS.holdoutShareMaxBps
      );
    case "targeting":
      if (!isRankingMode(draft.targetingMode)) return true;
      return (
        context.canRank &&
        draft.targetingShareBps >= LIMITS.targetingShareMinBps &&
        draft.targetingShareBps <= LIMITS.targetingShareMaxBps
      );
    case "review":
      return true;
  }
}

// Highest step index the user may reach: every step before it is complete.
export function reachableStepIndex(
  draft: CampaignDraft,
  context: StepContext,
): number {
  for (let index = 0; index < STEP_IDS.length - 1; index += 1) {
    if (!isStepComplete(STEP_IDS[index], draft, context)) return index;
  }
  return STEP_IDS.length - 1;
}
