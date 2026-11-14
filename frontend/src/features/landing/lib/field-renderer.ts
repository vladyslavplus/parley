// Canvas renderer for the customer field. One requestAnimationFrame loop,
// started and stopped by the owner (visibility), sized for devicePixelRatio,
// coloured from CSS tokens read off the canvas element itself.

import {
  WAVE,
  layoutParticles,
  particleCount,
  particleLight,
  waveAt,
  type Particle,
  type WaveState,
} from "./particles";

const RENDER = {
  /** Never render more than this many device pixels per CSS pixel. */
  maxPixelRatio: 2,
  /** Device-pixel budget for the whole field, so a wide screen stays smooth. */
  maxDevicePixels: 1_400_000,
  /** Dot radius as a share of the shorter field side, clamped in CSS px. */
  radiusShare: 1 / 240,
  minRadius: 1.4,
  maxRadius: 2.4,
  quietAlpha: 0.34,
  rippleAlpha: 0.42,
  holdoutAlpha: 0.7,
  haloAlpha: 0.2,
  haloScale: 4.2,
  litGrowth: 0.7,
  frontAlpha: 0.22,
  driftPx: 1.4,
  driftSpeed: 0.00035,
} as const;

/**
 * Canvas backing-store ratio: the screen's density, capped so the field never
 * paints more than the device-pixel budget on a large display.
 */
export function canvasPixelRatio(
  width: number,
  height: number,
  devicePixelRatio: number,
): number {
  if (width <= 0 || height <= 0) return 1;
  const budget = Math.sqrt(RENDER.maxDevicePixels / (width * height));
  return Math.max(
    1,
    Math.min(devicePixelRatio || 1, RENDER.maxPixelRatio, budget),
  );
}

export const FIELD_COLOR_TOKENS = {
  quiet: "--muted-foreground",
  lit: "--highlight",
  edge: "--landing-lit-edge",
} as const;

type FieldColors = Record<keyof typeof FIELD_COLOR_TOKENS, string>;

function readColors(element: Element): FieldColors {
  const style = getComputedStyle(element);
  const read = (token: string) => style.getPropertyValue(token).trim();
  return {
    quiet: read(FIELD_COLOR_TOKENS.quiet),
    lit: read(FIELD_COLOR_TOKENS.lit),
    edge: read(FIELD_COLOR_TOKENS.edge),
  };
}

export interface FieldRenderer {
  start(): void;
  stop(): void;
  resize(width: number, height: number): void;
  refreshColors(): void;
  destroy(): void;
}

export function createFieldRenderer(
  canvas: HTMLCanvasElement,
  onFirstFrame: () => void,
): FieldRenderer | null {
  const context = canvas.getContext("2d");
  if (!context) return null;
  const ctx = context;

  let width = 0;
  let height = 0;
  let radius: number = RENDER.minRadius;
  let particles: Particle[] = [];
  let colors = readColors(canvas);
  let frame = 0;
  let startedAt = 0;
  let pausedElapsed = 0;
  let drewOnce = false;

  function drawFront(wave: WaveState) {
    if (wave.sweeping === 0) return;
    // The front is the line x + slant * y = front, drawn as a soft stroke.
    ctx.globalAlpha = RENDER.frontAlpha;
    ctx.strokeStyle = colors.lit;
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(wave.front * width, 0);
    ctx.lineTo((wave.front - WAVE.slant) * width, height);
    ctx.stroke();
  }

  function draw(elapsed: number) {
    const wave = waveAt(elapsed);
    ctx.clearRect(0, 0, width, height);
    drawFront(wave);

    for (const particle of particles) {
      const { lit, ripple } = particleLight(particle, wave);
      const drift =
        Math.sin(elapsed * RENDER.driftSpeed + particle.phase) * RENDER.driftPx;
      const x = particle.x * width + drift;
      const y = particle.y * height;

      if (particle.holdout) {
        ctx.globalAlpha = RENDER.holdoutAlpha;
        ctx.strokeStyle = colors.quiet;
        ctx.lineWidth = 1;
        ctx.beginPath();
        ctx.arc(x, y, radius * 1.5, 0, Math.PI * 2);
        ctx.stroke();
        continue;
      }

      if (lit > 0) {
        ctx.globalAlpha = RENDER.haloAlpha * lit;
        ctx.fillStyle = colors.lit;
        ctx.beginPath();
        ctx.arc(x, y, radius * RENDER.haloScale, 0, Math.PI * 2);
        ctx.fill();
      }

      const quiet =
        (RENDER.quietAlpha + RENDER.rippleAlpha * ripple) * (1 - lit);
      if (quiet > 0) {
        ctx.globalAlpha = quiet;
        ctx.fillStyle = colors.quiet;
        ctx.beginPath();
        ctx.arc(x, y, radius * (1 + ripple * 0.4), 0, Math.PI * 2);
        ctx.fill();
      }

      if (lit > 0) {
        const r = radius * (1 + RENDER.litGrowth * lit);
        ctx.globalAlpha = lit;
        ctx.fillStyle = colors.lit;
        ctx.beginPath();
        ctx.arc(x, y, r, 0, Math.PI * 2);
        ctx.fill();
        ctx.strokeStyle = colors.edge;
        ctx.lineWidth = 1;
        ctx.stroke();
      }
    }
    ctx.globalAlpha = 1;

    if (!drewOnce) {
      drewOnce = true;
      onFirstFrame();
    }
  }

  function tick(now: number) {
    if (startedAt === 0) startedAt = now - pausedElapsed;
    pausedElapsed = now - startedAt;
    draw(pausedElapsed);
    frame = requestAnimationFrame(tick);
  }

  return {
    start() {
      if (frame !== 0 || width === 0) return;
      startedAt = 0;
      frame = requestAnimationFrame(tick);
    },
    stop() {
      if (frame === 0) return;
      cancelAnimationFrame(frame);
      frame = 0;
    },
    resize(nextWidth: number, nextHeight: number) {
      if (nextWidth === width && nextHeight === height) return;
      width = nextWidth;
      height = nextHeight;
      const ratio = canvasPixelRatio(
        width,
        height,
        window.devicePixelRatio || 1,
      );
      canvas.width = Math.round(width * ratio);
      canvas.height = Math.round(height * ratio);
      ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
      radius = Math.min(
        RENDER.maxRadius,
        Math.max(
          RENDER.minRadius,
          Math.min(width, height) * RENDER.radiusShare,
        ),
      );
      particles = layoutParticles(
        particleCount(width, height),
        height > 0 ? width / height : 1,
      );
      if (frame === 0 && width > 0) draw(pausedElapsed);
    },
    refreshColors() {
      colors = readColors(canvas);
      if (frame === 0 && width > 0) draw(pausedElapsed);
    },
    destroy() {
      this.stop();
      particles = [];
    },
  };
}
