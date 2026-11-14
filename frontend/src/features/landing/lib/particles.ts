// Pure model of the customer field: where each dot sits, which uplift group it
// belongs to, and how the campaign wave lights it over time. No DOM access, so
// the canvas renderer and the static SVG fallback draw the same scene.

export type ParticleGroup =
  "persuadable" | "sureThing" | "lostCause" | "sleepingDog";

export interface Particle {
  /** Horizontal position, 0..1 of the field width. */
  x: number;
  /** Vertical position, 0..1 of the field height. */
  y: number;
  group: ParticleGroup;
  /** Random holdout: never emailed, so never lit. */
  holdout: boolean;
  /** Drift phase in radians, keeps neighbours out of step. */
  phase: number;
}

export const FIELD = {
  seed: 20_261_107,
  minCount: 140,
  maxCount: 520,
  /** CSS pixels of field area per customer dot. */
  areaPerParticle: 1_500,
  /** Fixed count for the server-rendered fallback scene. */
  staticCount: 360,
  staticAspect: 16 / 9,
  /** Cumulative order matters: groups are assigned by one random draw. */
  groupShares: [
    ["persuadable", 0.17],
    ["sureThing", 0.23],
    ["lostCause", 0.45],
    ["sleepingDog", 0.15],
  ] as const satisfies readonly (readonly [ParticleGroup, number])[],
  holdoutShare: 0.1,
  /** How far inside its grid cell a dot may wander, 0..1. */
  jitter: 0.72,
} as const;

export const WAVE = {
  /** Width of the bright wave front, in field units. */
  width: 0.14,
  /** The front leans: lower dots are reached a little later. */
  slant: 0.32,
  sweepMs: 5_600,
  holdMs: 2_800,
  fadeMs: 1_500,
  restMs: 700,
} as const;

export const WAVE_CYCLE_MS =
  WAVE.sweepMs + WAVE.holdMs + WAVE.fadeMs + WAVE.restMs;

/** Where the front starts (fully left of every dot) and ends (past every dot). */
export const WAVE_START = -WAVE.width;
export const WAVE_END = 1 + WAVE.slant + WAVE.width;

/** Deterministic PRNG (mulberry32): same seed, same field on server and client. */
export function createRandom(seed: number): () => number {
  let state = seed >>> 0;
  return () => {
    state = (state + 0x6d2b79f5) >>> 0;
    let t = state;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4_294_967_296;
  };
}

/** Number of dots for a field of the given CSS size, capped both ways. */
export function particleCount(width: number, height: number): number {
  if (width <= 0 || height <= 0) return 0;
  const raw = Math.round((width * height) / FIELD.areaPerParticle);
  return Math.min(FIELD.maxCount, Math.max(FIELD.minCount, raw));
}

function pickGroup(draw: number): ParticleGroup {
  let edge = 0;
  for (const [group, share] of FIELD.groupShares) {
    edge += share;
    if (draw < edge) return group;
  }
  return FIELD.groupShares[FIELD.groupShares.length - 1][0];
}

/**
 * Jittered grid: even coverage without visible rows. The grid follows the
 * aspect ratio so dots stay round-spaced on a phone and on a wide screen.
 */
export function layoutParticles(
  count: number,
  aspect: number,
  seed: number = FIELD.seed,
): Particle[] {
  if (count <= 0) return [];
  const safeAspect = aspect > 0 ? aspect : 1;
  const columns = Math.max(1, Math.round(Math.sqrt(count * safeAspect)));
  const rows = Math.ceil(count / columns);
  const random = createRandom(seed);
  const particles: Particle[] = [];

  for (let index = 0; index < count; index++) {
    const column = index % columns;
    const row = Math.floor(index / columns);
    const x = (column + 0.5 + (random() - 0.5) * FIELD.jitter) / columns;
    const y = (row + 0.5 + (random() - 0.5) * FIELD.jitter) / rows;
    particles.push({
      x,
      y,
      group: pickGroup(random()),
      holdout: random() < FIELD.holdoutShare,
      phase: random() * Math.PI * 2,
    });
  }
  return particles;
}

export function isTarget(particle: Particle): boolean {
  return particle.group === "persuadable" && !particle.holdout;
}

function clamp01(value: number): number {
  return Math.min(1, Math.max(0, value));
}

function smoothstep(edge0: number, edge1: number, value: number): number {
  const t = clamp01((value - edge0) / (edge1 - edge0));
  return t * t * (3 - 2 * t);
}

function easeInOut(t: number): number {
  return t < 0.5 ? 4 * t * t * t : 1 - (-2 * t + 2) ** 3 / 2;
}

export interface WaveState {
  /** Position of the front along x + slant * y. */
  front: number;
  /** How much of the lit state remains, 1 while the result holds. */
  glow: number;
  /** 1 while the wave is moving, used to show the front itself. */
  sweeping: number;
}

/** The final, settled scene: the wave has passed and every target is lit. */
export const SETTLED_WAVE: WaveState = {
  front: WAVE_END,
  glow: 1,
  sweeping: 0,
};

/** Wave state at a moment of the repeating campaign cycle. */
export function waveAt(elapsedMs: number): WaveState {
  const t = ((elapsedMs % WAVE_CYCLE_MS) + WAVE_CYCLE_MS) % WAVE_CYCLE_MS;
  if (t < WAVE.sweepMs) {
    const progress = easeInOut(t / WAVE.sweepMs);
    return {
      front: WAVE_START + (WAVE_END - WAVE_START) * progress,
      glow: 1,
      sweeping: 1,
    };
  }
  const afterSweep = t - WAVE.sweepMs;
  if (afterSweep < WAVE.holdMs) return SETTLED_WAVE;
  const afterHold = afterSweep - WAVE.holdMs;
  if (afterHold < WAVE.fadeMs) {
    return { front: WAVE_END, glow: 1 - afterHold / WAVE.fadeMs, sweeping: 0 };
  }
  return { front: WAVE_START, glow: 0, sweeping: 0 };
}

export interface ParticleLight {
  /** 0..1, how strongly the dot shows the highlight colour. */
  lit: number;
  /** 0..1, brief brightening as the front passes any reached dot. */
  ripple: number;
}

/** How one dot looks for a given wave state. Holdout dots are never reached. */
export function particleLight(
  particle: Particle,
  wave: WaveState,
): ParticleLight {
  if (particle.holdout) return { lit: 0, ripple: 0 };
  const passed = wave.front - (particle.x + WAVE.slant * particle.y);
  const ripple =
    wave.sweeping > 0
      ? clamp01(1 - Math.abs(passed - WAVE.width / 2) / (WAVE.width / 2))
      : 0;
  const lit = isTarget(particle)
    ? smoothstep(0, WAVE.width, passed) * wave.glow
    : 0;
  return { lit, ripple };
}
