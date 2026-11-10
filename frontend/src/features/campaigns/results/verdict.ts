import type { AlertTone } from "@/components/ui/alert";
import {
  CAMPAIGN_SOURCES,
  RESULT_VERDICTS,
  WINDOW_STATES,
  toNumber,
  type CampaignResults,
  type ResultVerdict,
} from "@/features/campaigns/types";

export interface ResultView {
  // Which wording to use. The server's verdict, except that a sample ratio
  // alarm always reads as unreliable. Unknown values fall back to "too early".
  verdict: ResultVerdict;
  // Figures can still change: the window is open, not started, or closed
  // without final figures yet. While provisional no comparison is shown.
  provisional: boolean;
  // The window has closed but the final figures are not computed yet.
  finalizing: boolean;
  // The window is still open (the figures are provisional).
  windowOpen: boolean;
  // The attribution job has computed the figures. Before that there is no
  // result to show, only empty placeholders.
  calculated: boolean;
  // The server's sample ratio check alarmed: the lift is unreliable.
  unreliable: boolean;
  simulated: boolean;
  // Seeded from the historic dataset: no email was sent.
  demo: boolean;
  // The interval can be drawn: both bounds and the lift exist.
  hasInterval: boolean;
}

const KNOWN: readonly string[] = Object.values(RESULT_VERDICTS);

// Nothing is computed when the job never ran, or when neither arm has anyone
// to compare: the figures are then placeholders, not a result.
function isCalculated(results: CampaignResults): boolean {
  if (results.computedAt === null) return false;
  return (
    toNumber(results.treatment.targeted) + toNumber(results.holdout.targeted) >
    0
  );
}

export function resultView(results: CampaignResults): ResultView {
  const known: ResultVerdict = KNOWN.includes(results.verdict)
    ? (results.verdict as ResultVerdict)
    : RESULT_VERDICTS.tooEarly;
  // Only the server's final flag counts; open, not started and awaiting-final
  // windows are all provisional.
  const isFinal =
    results.isFinal && results.windowState === WINDOW_STATES.closed;
  return {
    verdict: results.srmWarning ? RESULT_VERDICTS.unreliable : known,
    provisional: !isFinal,
    finalizing: results.windowState === WINDOW_STATES.awaitingFinal,
    windowOpen: results.windowState === WINDOW_STATES.open,
    calculated: isCalculated(results),
    unreliable: results.srmWarning,
    simulated: results.simulated,
    demo: results.source === CAMPAIGN_SOURCES.demoSeed,
    hasInterval:
      results.lift !== null &&
      results.liftCiLow !== null &&
      results.liftCiHigh !== null,
  };
}

// Styling of the verdict text. Nothing reads as a conclusion while the figures
// are provisional or inconclusive, and an unreliable result never looks like
// success.
export function verdictTone(view: ResultView): AlertTone {
  if (view.verdict === RESULT_VERDICTS.unreliable) return "warning";
  if (view.provisional || view.verdict === RESULT_VERDICTS.tooEarly) {
    return "neutral";
  }
  return "info";
}

// Percent points of a fraction, for display ("0.049" -> 4.9).
export function toPercentPoints(
  fraction: number | string | null,
): number | null {
  if (fraction === null) return null;
  return toNumber(fraction) * 100;
}
