import type { SVGProps } from "react";

/** Colours the mark is painted with: CSS variables in the UI, literals elsewhere. */
export interface LogoPaint {
  ink: string;
  lit: string;
  plateHigh: string;
  plateLow: string;
  plateEdge: string;
  litHigh: string;
  litMid: string;
  litLow: string;
  shade: string;
  sheen: string;
}

/** Paint that reads the brand tokens from the surrounding theme. */
export const LOGO_TOKEN_PAINT: LogoPaint = {
  ink: "var(--logo-ink)",
  lit: "var(--logo-lit)",
  plateHigh: "var(--logo-plate-high)",
  plateLow: "var(--logo-plate-low)",
  plateEdge: "var(--logo-plate-edge)",
  litHigh: "var(--logo-lit-high)",
  litMid: "var(--logo-lit-mid)",
  litLow: "var(--logo-lit-low)",
  shade: "var(--logo-shade)",
  sheen: "var(--logo-sheen)",
};

/** Tier names: the full mark has depth, the flat mark is two solid shapes. */
export type LogoTier = "full" | "flat";

/** Below this rendered size the hairlines of the full tier go grey. */
export const LOGO_FLAT_BELOW_PX = 24;

export const LOGO_VIEW_BOX: Record<LogoTier, number> = { full: 100, flat: 24 };

const RADIUS = 9.375;
const PLATE_DROP = 3.2;
const SLAB = { x: 12.5, y: 56.73, width: 75, height: 29.17 } as const;
const BLOCK = { x: 54.17, y: 10.9, size: 33.33 } as const;
/** The cast shadow starts a little below the slab's top edge. */
const SHADOW_CLIP_Y = 58.33;

const FLAT_RADIUS = 2.25;

interface ArtProps {
  /** Prefix that keeps gradient, clip and filter ids unique per instance. */
  idPrefix: string;
  paint: LogoPaint;
}

/* The art is built by plain functions returning one <g>: the social image
   renderer accepts only intrinsic elements inside an <svg>. */
function flatArt({ paint }: Pick<ArtProps, "paint">) {
  return (
    <g>
      <rect
        x={3}
        y={14}
        width={18}
        height={7}
        rx={FLAT_RADIUS}
        fill={paint.ink}
      />
      <rect
        x={13}
        y={3}
        width={8}
        height={8}
        rx={FLAT_RADIUS}
        fill={paint.lit}
      />
    </g>
  );
}

function fullArt({ idPrefix, paint }: ArtProps) {
  const slabClip = `${idPrefix}-slab`;
  const shadowClip = `${idPrefix}-shadow`;
  const blockClip = `${idPrefix}-block`;
  const plateBlur = `${idPrefix}-plate-blur`;
  const castBlur = `${idPrefix}-cast-blur`;
  const slabFill = `${idPrefix}-slab-fill`;
  const blockFill = `${idPrefix}-block-fill`;

  return (
    <g>
      <defs>
        <clipPath id={slabClip}>
          <rect {...SLAB} rx={RADIUS} />
        </clipPath>
        <clipPath id={shadowClip}>
          <rect {...SLAB} y={SHADOW_CLIP_Y} rx={RADIUS} />
        </clipPath>
        <clipPath id={blockClip}>
          <rect
            x={BLOCK.x}
            y={BLOCK.y}
            width={BLOCK.size}
            height={BLOCK.size}
            rx={RADIUS}
          />
        </clipPath>
        <filter
          id={plateBlur}
          x="-60%"
          y="-60%"
          width="220%"
          height="220%"
          colorInterpolationFilters="sRGB"
        >
          <feGaussianBlur stdDeviation={2.6} />
        </filter>
        <filter
          id={castBlur}
          x="-60%"
          y="-60%"
          width="220%"
          height="220%"
          colorInterpolationFilters="sRGB"
        >
          <feGaussianBlur stdDeviation={3.4} />
        </filter>
        <linearGradient
          id={slabFill}
          gradientUnits="userSpaceOnUse"
          x1={SLAB.x}
          y1={SLAB.y}
          x2={42.5}
          y2={85.9}
        >
          <stop offset={0} stopColor={paint.plateHigh} />
          <stop offset={1} stopColor={paint.plateLow} />
        </linearGradient>
        <linearGradient
          id={blockFill}
          gradientUnits="userSpaceOnUse"
          x1={BLOCK.x}
          y1={BLOCK.y}
          x2={70.83}
          y2={44.23}
        >
          <stop offset={0} stopColor={paint.litHigh} />
          <stop offset={0.45} stopColor={paint.litMid} />
          <stop offset={1} stopColor={paint.litLow} />
        </linearGradient>
      </defs>

      {/* Soft shadow of the slab on the page. */}
      <rect
        x={SLAB.x + 1.5}
        y={SLAB.y + 7.3}
        width={SLAB.width}
        height={SLAB.height}
        rx={RADIUS}
        fill={paint.shade}
        fillOpacity={0.22}
        filter={`url(#${plateBlur})`}
      />
      {/* Lower edge of the slab. */}
      <rect
        x={SLAB.x}
        y={SLAB.y + PLATE_DROP}
        width={SLAB.width}
        height={SLAB.height}
        rx={RADIUS}
        fill={paint.plateEdge}
      />
      <rect {...SLAB} rx={RADIUS} fill={`url(#${slabFill})`} />
      <rect
        {...SLAB}
        rx={RADIUS}
        fill="none"
        stroke={paint.sheen}
        strokeOpacity={0.2}
        strokeWidth={1.4}
        clipPath={`url(#${slabClip})`}
      />
      {/* Cast shadow of the lifted block on the slab. */}
      <g clipPath={`url(#${shadowClip})`}>
        <ellipse
          cx={73.83}
          cy={SLAB.y}
          rx={19.67}
          ry={7}
          fill={paint.shade}
          fillOpacity={0.42}
          filter={`url(#${castBlur})`}
        />
      </g>
      {/* Lit block: lower edge, face and light edge. */}
      <rect
        x={BLOCK.x}
        y={BLOCK.y + PLATE_DROP}
        width={BLOCK.size}
        height={BLOCK.size}
        rx={RADIUS}
        fill={paint.litLow}
      />
      <rect
        x={BLOCK.x}
        y={BLOCK.y}
        width={BLOCK.size}
        height={BLOCK.size}
        rx={RADIUS}
        fill={`url(#${blockFill})`}
      />
      <rect
        x={BLOCK.x}
        y={BLOCK.y}
        width={BLOCK.size}
        height={BLOCK.size}
        rx={RADIUS}
        fill="none"
        stroke={paint.sheen}
        strokeOpacity={0.5}
        strokeWidth={1.4}
        clipPath={`url(#${blockClip})`}
      />
    </g>
  );
}

export interface LogoArtProps extends Omit<
  SVGProps<SVGSVGElement>,
  "viewBox" | "children"
> {
  tier: LogoTier;
  idPrefix: string;
  paint?: LogoPaint;
}

/**
 * The Lift mark as one inline SVG: a graphite slab with a lit block raised
 * above it. Pure and hook-free so the social image can render it too.
 */
export function LogoArt({
  tier,
  idPrefix,
  paint = LOGO_TOKEN_PAINT,
  ...svgProps
}: LogoArtProps) {
  const box = LOGO_VIEW_BOX[tier];
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox={`0 0 ${box} ${box}`}
      {...svgProps}
    >
      {tier === "full" ? fullArt({ idPrefix, paint }) : flatArt({ paint })}
    </svg>
  );
}
