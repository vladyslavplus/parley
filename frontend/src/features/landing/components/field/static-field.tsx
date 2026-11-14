import {
  FIELD,
  SETTLED_WAVE,
  layoutParticles,
  particleLight,
} from "../../lib/particles";

const VIEW_WIDTH = 1600;
const VIEW_HEIGHT = Math.round(VIEW_WIDTH / FIELD.staticAspect);
const DOT = 3;

type SceneGroup = "quiet" | "halo" | "lit" | "holdout";

const RADIUS: Record<SceneGroup, number> = {
  quiet: DOT,
  halo: DOT * 4.2,
  lit: DOT * 1.7,
  holdout: DOT * 1.5,
};

const PAINT: Record<SceneGroup, { className: string; strokeWidth?: number }> = {
  quiet: { className: "fill-muted-foreground opacity-35" },
  halo: { className: "fill-highlight opacity-20" },
  lit: {
    className: "fill-highlight stroke-landing-lit-edge",
    strokeWidth: 1.2,
  },
  holdout: {
    className: "fill-none stroke-muted-foreground opacity-70",
    strokeWidth: 1.4,
  },
};

/** A full circle as one closed subpath, so many dots fit in a single path. */
function circle(x: number, y: number, radius: number): string {
  return `M${x - radius} ${y}a${radius} ${radius} 0 1 0 ${radius * 2} 0a${radius} ${radius} 0 1 0 ${-radius * 2} 0`;
}

/**
 * The settled field, grouped by paint and computed once per server process:
 * four paths instead of one node per dot, which keeps the markup and the
 * first style pass small without changing what the scene looks like.
 */
const SCENE = ((): Record<SceneGroup, string> => {
  const paths: Record<SceneGroup, string[]> = {
    quiet: [],
    halo: [],
    lit: [],
    holdout: [],
  };
  for (const particle of layoutParticles(
    FIELD.staticCount,
    FIELD.staticAspect,
  )) {
    const x = Math.round(particle.x * VIEW_WIDTH);
    const y = Math.round(particle.y * VIEW_HEIGHT);
    const lit = !particle.holdout
      ? particleLight(particle, SETTLED_WAVE).lit > 0
      : false;
    const group: SceneGroup = particle.holdout
      ? "holdout"
      : lit
        ? "lit"
        : "quiet";
    if (group === "lit") paths.halo.push(circle(x, y, RADIUS.halo));
    paths[group].push(circle(x, y, RADIUS[group]));
  }
  return {
    quiet: paths.quiet.join(""),
    halo: paths.halo.join(""),
    lit: paths.lit.join(""),
    holdout: paths.holdout.join(""),
  };
})();

const DRAW_ORDER = ["halo", "quiet", "lit", "holdout"] as const;

/** The final lit state of the field: what reduced motion and no-JS see. */
export function StaticField({ className }: { className?: string }) {
  return (
    <svg
      viewBox={`0 0 ${VIEW_WIDTH} ${VIEW_HEIGHT}`}
      preserveAspectRatio="xMidYMid slice"
      className={className}
      focusable="false"
    >
      {DRAW_ORDER.map((group) =>
        SCENE[group] ? (
          <path
            key={group}
            d={SCENE[group]}
            className={PAINT[group].className}
            strokeWidth={PAINT[group].strokeWidth}
          />
        ) : null,
      )}
    </svg>
  );
}
