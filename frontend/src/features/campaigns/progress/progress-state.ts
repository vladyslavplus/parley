import { CAMPAIGN_STATUSES, toNumber } from "@/features/campaigns/types";
import type { CampaignProgress } from "@/features/campaigns/types";

export interface ProgressSummary {
  total: number;
  queued: number;
  sent: number;
  failed: number;
  unknown: number;
  suppressed: number;
  // Recipients no longer waiting: sent, failed, unknown or suppressed.
  processed: number;
  // Whole percent of `processed` in `total`, 0 when nothing is planned.
  percent: number;
}

export function summarizeProgress(progress: CampaignProgress): ProgressSummary {
  const total = toNumber(progress.total);
  const queued = toNumber(progress.queued);
  const sent = toNumber(progress.sent);
  const failed = toNumber(progress.failed);
  const unknown = toNumber(progress.unknown);
  const suppressed = toNumber(progress.suppressed);
  const processed = sent + failed + unknown + suppressed;
  const percent =
    total > 0 ? Math.min(100, Math.floor((processed * 100) / total)) : 0;
  return {
    total,
    queued,
    sent,
    failed,
    unknown,
    suppressed,
    processed,
    percent,
  };
}

// Polling continues only while the campaign is still sending.
export function shouldPollProgress(status: string | undefined): boolean {
  return status === CAMPAIGN_STATUSES.sending;
}

// Statuses that have a delivery to show.
export function hasDelivery(status: string): boolean {
  return (
    status === CAMPAIGN_STATUSES.sending ||
    status === CAMPAIGN_STATUSES.completed ||
    status === CAMPAIGN_STATUSES.failed ||
    status === CAMPAIGN_STATUSES.cancelled
  );
}
