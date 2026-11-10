import type { AlertTone } from "@/components/ui/alert";
import { WINDOW_STATES } from "@/features/campaigns/types";
import type { LatestCampaign } from "@/features/insights/types";

// Verdict wording keys of the dashboard widget. Same enum and same honest copy
// as the campaign results panel; an unknown value falls back to "too early".
export const LATEST_VERDICT_KEYS = {
  measurable_lift: "verdict.measurable_lift",
  no_measurable_effect: "verdict.no_measurable_effect",
  too_early: "verdict.too_early",
  unreliable: "verdict.unreliable",
} as const;

export type LatestVerdictKey =
  (typeof LATEST_VERDICT_KEYS)[keyof typeof LATEST_VERDICT_KEYS];

export interface LatestCampaignState {
  verdictKey: LatestVerdictKey;
  // Only the server's final flag makes the figures final. Open, not started
  // and awaiting-final windows are all provisional.
  provisional: boolean;
  // The window has closed and the final figures are being calculated.
  finalizing: boolean;
  // Lift and interval are not shown: provisional or unreliable figures.
  withheld: boolean;
  tone: AlertTone;
}

const KNOWN = Object.keys(LATEST_VERDICT_KEYS) as Array<
  keyof typeof LATEST_VERDICT_KEYS
>;

export function latestCampaignState(
  campaign: Pick<LatestCampaign, "verdict" | "isFinal" | "windowState">,
): LatestCampaignState {
  const known = (KNOWN as string[]).includes(campaign.verdict)
    ? (campaign.verdict as keyof typeof LATEST_VERDICT_KEYS)
    : "too_early";
  const verdictKey = LATEST_VERDICT_KEYS[known];
  const provisional = !campaign.isFinal;
  const finalizing = campaign.windowState === WINDOW_STATES.awaitingFinal;
  const unreliable = known === "unreliable";
  let tone: AlertTone = "info";
  if (unreliable) tone = "warning";
  else if (provisional || known === "too_early") tone = "neutral";
  return {
    verdictKey,
    provisional,
    finalizing,
    withheld: provisional || unreliable,
    tone,
  };
}
