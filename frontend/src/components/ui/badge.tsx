import type { LucideIcon } from "lucide-react";
import type { ReactNode } from "react";

import { cn } from "@/lib/utils";

export type BadgeTone = "info" | "success" | "warning" | "neutral" | "strong";
export type BadgeVariant = "filled" | "outline";
export type BadgeShape = "segment" | "pill";

// The one tone map for both tiers, built like `Alert`: a 40 % border, a 10 %
// tint and the ink at full strength. "neutral" is graphite (secondary),
// "strong" keeps graphite but lifts the text to the foreground for facts that
// must not fade. Tints stay at 10 % so the ink keeps AA contrast on `bg-card`
// in both themes, and every badge also carries an icon and a word, so colour is
// never the only carrier of the meaning.
const TONE_CLASSES: Record<BadgeTone, Record<BadgeVariant, string>> = {
  info: {
    filled: "border-info/40 bg-info/10 text-info",
    outline: "border-info/40 bg-transparent text-info",
  },
  success: {
    filled: "border-success/40 bg-success/10 text-success",
    outline: "border-success/40 bg-transparent text-success",
  },
  warning: {
    filled: "border-warning/40 bg-warning/10 text-warning",
    outline: "border-warning/40 bg-transparent text-warning",
  },
  neutral: {
    filled: "border-border bg-muted text-muted-foreground",
    outline: "border-border bg-transparent text-muted-foreground",
  },
  strong: {
    filled: "border-foreground/20 bg-muted text-foreground",
    outline: "border-foreground/20 bg-transparent text-foreground",
  },
};

// A segment badge names what a row is (6 px radius, so it cannot be confused
// with a reason chip); a chip explains why (pill).
const SHAPE_CLASSES: Record<BadgeShape, string> = {
  segment: "rounded-[var(--radius-control)] px-2 py-0.5",
  pill: "rounded-full px-2 py-0.5",
};

export interface BadgeProps {
  tone?: BadgeTone;
  variant?: BadgeVariant;
  shape?: BadgeShape;
  /** Leading mark of the tone group, 12 px, always with a word next to it. */
  icon?: LucideIcon;
  children: ReactNode;
  className?: string;
  /** Native tooltip for the longer rule behind the word. */
  title?: string;
}

export function Badge({
  tone = "neutral",
  variant = "filled",
  shape = "segment",
  icon: Icon,
  children,
  className,
  title,
}: BadgeProps) {
  return (
    <span
      title={title}
      className={cn(
        "inline-flex max-w-full items-center gap-1 border text-[11px] font-medium leading-5 whitespace-nowrap",
        TONE_CLASSES[tone][variant],
        SHAPE_CLASSES[shape],
        className,
      )}
    >
      {Icon ? <Icon className="h-3 w-3 shrink-0" aria-hidden="true" /> : null}
      <span className="truncate">{children}</span>
    </span>
  );
}
