import {
  computeContrastRatio,
  STAGE_DEFAULT_COLORS,
  STAGE_INK,
  STAGE_PAPER,
  THEME_PAGE_BACKGROUNDS,
} from "./tokens";

/** The stored format of a stage colour: a six-digit hex triplet. */
export const HEX_COLOR_PATTERN = /^#[0-9a-fA-F]{6}$/;

export const HEX_COLOR_LENGTH = 7;

// A custom stage colour below this ratio against a page background is hard to
// tell apart from the surface it sits on.
export const MIN_STAGE_COLOR_CONTRAST = 3;

// Constant tokens: unlike `--stage-1-foreground`, they do not flip with the
// theme, because the stage colour they contrast against does not either.
export const STAGE_INK_FOREGROUND = "var(--stage-ink)";
export const STAGE_PAPER_FOREGROUND = "var(--stage-paper)";

export type ThemeName = keyof typeof THEME_PAGE_BACKGROUNDS;

export function isHexColor(value: string): boolean {
  return HEX_COLOR_PATTERN.test(value.trim());
}

export function isDefaultStageColor(value: string): boolean {
  return (STAGE_DEFAULT_COLORS as readonly string[]).includes(
    value.trim().toLowerCase(),
  );
}

/**
 * Lowercases a valid hex triplet and adds the leading `#` when it is missing.
 * Anything that is not a full hex triplet is returned trimmed, so the form
 * validation can show the error instead of silently changing the input.
 */
export function normalizeHexColor(value: string): string {
  const trimmed = value.trim();
  if (!trimmed) return trimmed;
  const withHash = trimmed.startsWith("#") ? trimmed : `#${trimmed}`;
  return HEX_COLOR_PATTERN.test(withHash) ? withHash.toLowerCase() : trimmed;
}

/** A colour that is safe to paint: the stored hex or the first default. */
export function safeStageColor(value: string | undefined | null): string {
  return value && isHexColor(value)
    ? value.trim().toLowerCase()
    : STAGE_DEFAULT_COLORS[0];
}

/**
 * Picks the text colour for text drawn on a stage colour: whichever of the ink
 * and paper tokens has the higher WCAG contrast against it.
 */
export function pickStageForeground(color: string): string {
  const safe = safeStageColor(color);
  const ink = computeContrastRatio(safe, STAGE_INK);
  const paper = computeContrastRatio(safe, STAGE_PAPER);
  return paper > ink ? STAGE_PAPER_FOREGROUND : STAGE_INK_FOREGROUND;
}

/** Inline style for a surface painted with a stored stage colour. */
export function stageColorStyle(color: string): { backgroundColor: string } {
  return { backgroundColor: safeStageColor(color) };
}

/** WCAG contrast of a stage colour against each theme page background. */
export function stageColorBackgroundContrasts(
  color: string,
): Record<ThemeName, number> {
  const safe = safeStageColor(color);
  return {
    light: computeContrastRatio(safe, THEME_PAGE_BACKGROUNDS.light),
    dark: computeContrastRatio(safe, THEME_PAGE_BACKGROUNDS.dark),
  };
}

/**
 * The themes where a stage colour blends into the page background. The five
 * default colours are reviewed design values, so they are never reported.
 */
export function stageColorLowContrastThemes(color: string): ThemeName[] {
  if (isDefaultStageColor(color)) return [];
  const contrasts = stageColorBackgroundContrasts(color);
  return (Object.keys(contrasts) as ThemeName[]).filter(
    (theme) => contrasts[theme] < MIN_STAGE_COLOR_CONTRAST,
  );
}
