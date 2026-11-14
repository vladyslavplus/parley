import { APP_NAME } from "@/constants/app";
import { cn } from "@/lib/utils";

import { LogoMark, LOGO_MARK_DEFAULT_SIZE } from "./logo-mark";

/** Lockup proportions from the brand grid, in units of the mark height. */
const GAP_RATIO = 7 / 24;
/** Source Serif 4 caps are about two thirds of the em; the lockup caps are 13/24. */
const FONT_SIZE_RATIO = 0.81;

export interface LogoProps {
  /** Height of the mark in pixels; the word scales with it. */
  size?: number;
  className?: string;
}

/**
 * Mark plus the live "Parley" word in the display font. The word names the
 * lockup for assistive tech, so the mark stays decorative.
 */
export function Logo({ size = LOGO_MARK_DEFAULT_SIZE, className }: LogoProps) {
  return (
    <span
      className={cn(
        "inline-flex items-center font-display font-semibold leading-none tracking-[-0.01em] text-foreground",
        className,
      )}
      style={{ gap: size * GAP_RATIO, fontSize: size * FONT_SIZE_RATIO }}
    >
      <LogoMark size={size} />
      <span>{APP_NAME}</span>
    </span>
  );
}
