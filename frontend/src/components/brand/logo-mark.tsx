import { useId } from "react";

import { cn } from "@/lib/utils";

import { LOGO_FLAT_BELOW_PX, LogoArt, type LogoTier } from "./logo-art";

export const LOGO_MARK_DEFAULT_SIZE = 24;

export interface LogoMarkProps {
  /** Rendered width and height in pixels. */
  size?: number;
  /** `auto` picks the flat tier below 24 px, where the full tier blurs. */
  variant?: "auto" | LogoTier;
  /** Accessible name. Without it the mark is decorative and hidden. */
  title?: string;
  className?: string;
}

export function logoTier(
  size: number,
  variant: LogoMarkProps["variant"] = "auto",
): LogoTier {
  if (variant !== "auto") return variant;
  return size < LOGO_FLAT_BELOW_PX ? "flat" : "full";
}

/** The Parley mark, coloured only by theme tokens. */
export function LogoMark({
  size = LOGO_MARK_DEFAULT_SIZE,
  variant = "auto",
  title,
  className,
}: LogoMarkProps) {
  const idPrefix = `logo-${useId().replace(/[^a-zA-Z0-9]/g, "")}`;
  const tier = logoTier(size, variant);

  return (
    <LogoArt
      tier={tier}
      idPrefix={idPrefix}
      width={size}
      height={size}
      data-tier={tier}
      className={cn("block shrink-0", className)}
      {...(title
        ? { role: "img", "aria-label": title }
        : { "aria-hidden": true, focusable: false })}
    />
  );
}
