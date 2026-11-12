import type { Schemas } from "@/lib/api/schemas";

export type Template = Schemas["TemplateView"];
export type TemplateSummary = Schemas["TemplateSummaryView"];
export type TemplatePage = Schemas["KeysetCursorPageOfTemplateSummaryView"];
export type TemplatePreview = Schemas["TemplatePreviewView"];

export type Segment = Schemas["SegmentView"];
export type SegmentSummary = Schemas["SegmentSummaryView"];
export type SegmentPage = Schemas["KeysetCursorPageOfSegmentSummaryView"];
export type SegmentPreview = Schemas["SegmentPreviewView"];
export type SegmentDefinition = Schemas["SegmentDefinition"];

export type Campaign = Schemas["CampaignView"];
export type CampaignReview = Schemas["CampaignReviewView"];
export type CampaignProgress = Schemas["CampaignProgressView"];
export type CampaignResults = Schemas["CampaignResultsView"];
export type CampaignResultsArm = Schemas["CampaignResultsArmView"];
export type CampaignRecipient = Schemas["CampaignRecipientView"];
export type CampaignRecipientPage =
  Schemas["KeysetCursorPageOfCampaignRecipientView"];

// The conversion of a recipient whose outcome the viewer may not see.
export const CONVERSION_NOT_VISIBLE = "not_visible";

// The contact was deleted after the campaign: its outcome is hidden.
export const CONVERSION_DELETED = "deleted";

// Server-computed verdict of a campaign result; the UI only chooses wording.
// `tooEarly` is also the fallback for a value the UI does not know.
export const RESULT_VERDICTS = {
  measurableLift: "measurable_lift",
  noMeasurableEffect: "no_measurable_effect",
  tooEarly: "too_early",
  unreliable: "unreliable",
} as const;

// Who created a campaign (`source` of the results). Demo campaigns were
// seeded from a historic dataset and never sent an email.
export const CAMPAIGN_SOURCES = {
  user: "user",
  demoSeed: "demo_seed",
} as const;
export type ResultVerdict =
  (typeof RESULT_VERDICTS)[keyof typeof RESULT_VERDICTS];

// `awaiting_final`: the window has closed, the final figures are not computed.
export const WINDOW_STATES = {
  notStarted: "not_started",
  open: "open",
  awaitingFinal: "awaiting_final",
  closed: "closed",
} as const;
export type CampaignSummary = Schemas["CampaignSummaryView"];
export type CampaignPage = Schemas["KeysetCursorPageOfCampaignSummaryView"];

export interface CampaignFilters {
  q?: string;
  status?: string;
  sort?: string;
  order?: string;
  limit?: number;
  after?: string | null;
}

export const TARGETING_MODES = ["all", "top_uplift", "top_rfm"] as const;
export type TargetingMode = (typeof TARGETING_MODES)[number];

export const CAMPAIGN_STATUSES = {
  draft: "draft",
  preparing: "preparing",
  ready: "ready",
  sending: "sending",
  completed: "completed",
  cancelled: "cancelled",
  failed: "failed",
} as const;
export type CampaignStatus =
  (typeof CAMPAIGN_STATUSES)[keyof typeof CAMPAIGN_STATUSES];
export const CAMPAIGN_STATUS_LIST = Object.values(CAMPAIGN_STATUSES);

export const TEMPLATE_LOCALES = ["en", "uk"] as const;
export type TemplateLocale = (typeof TEMPLATE_LOCALES)[number];

// The only variables a template may reference (enforced by the server).
export const TEMPLATE_VARIABLES = [
  "contact.first_name",
  "contact.last_name",
  "company.name",
  "unsubscribe_url",
] as const;

// Liquid tags and filters the server allows; shown in the editor help.
export const TEMPLATE_TAGS = [
  "if / elsif / else / endif",
  "unless / endunless",
  "case / when / endcase",
  "for / endfor / break / continue / cycle",
  "raw / endraw",
  "comment / endcomment",
] as const;
export const TEMPLATE_FILTERS = [
  "default",
  "escape",
  "upcase",
  "downcase",
  "capitalize",
  "strip",
  "truncate",
  "truncatewords",
  "date",
] as const;

export interface TemplateFilters {
  q?: string;
  limit?: number;
  after?: string | null;
}

export interface SegmentFilters {
  q?: string;
  limit?: number;
  after?: string | null;
}

export function toNumber(value: number | string | null | undefined): number {
  if (value === null || value === undefined) return 0;
  return typeof value === "number" ? value : Number(value);
}
