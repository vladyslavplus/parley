import {
  BadgeDollarSign,
  BellOff,
  Check,
  Circle,
  Clock,
  Handshake,
  ListTodo,
  Minus,
  Target,
  TrendingUp,
  type LucideIcon,
} from "lucide-react";

import type { BadgeTone, BadgeVariant } from "@/components/ui/badge";

export interface BadgeStyle {
  tone: BadgeTone;
  variant: BadgeVariant;
  icon: LucideIcon;
}

// What a row is: the model's uplift decision first, the RFM segment as a quiet
// secondary label. Each value carries a distinct word and icon, so the tint is
// never the only difference between two segments.
export const UPLIFT_SEGMENT_STYLES: Record<string, BadgeStyle> = {
  persuadable: { tone: "info", variant: "filled", icon: Target },
  sure_thing: { tone: "neutral", variant: "filled", icon: Check },
  lost_cause: { tone: "neutral", variant: "outline", icon: Minus },
  do_not_disturb: { tone: "warning", variant: "filled", icon: BellOff },
};

// Why a row is here. One tone group per kind of fact: a model signal, a
// positive fact, a CRM action, a caution.
export const REASON_STYLES: Record<string, BadgeStyle> = {
  "reason.uplift_high": { tone: "info", variant: "outline", icon: TrendingUp },
  "reason.high_value": {
    tone: "success",
    variant: "outline",
    icon: BadgeDollarSign,
  },
  "reason.recent_order": { tone: "success", variant: "outline", icon: Clock },
  "reason.task_due": { tone: "strong", variant: "filled", icon: ListTodo },
  "reason.deal_stale": { tone: "strong", variant: "filled", icon: Handshake },
  "reason.do_not_disturb": {
    tone: "warning",
    variant: "filled",
    icon: BellOff,
  },
};

// Old priority days still carry these; the segment badge already says it, so
// they are dropped silently instead of rendering a duplicate chip.
export const RETIRED_REASON_CODES: ReadonlySet<string> = new Set([
  "reason.at_risk",
  "reason.persuadable",
]);

// A code no build knows yet still gets a readable, neutral chip.
export const UNKNOWN_REASON_STYLE: BadgeStyle = {
  tone: "neutral",
  variant: "filled",
  icon: Circle,
};

/** The raw code with its prefix and underscores removed, for an unknown chip. */
export function humanizeReasonCode(code: string): string {
  return code
    .replace(/^reason\./, "")
    .replace(/_/g, " ")
    .trim();
}
