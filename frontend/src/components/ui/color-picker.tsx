"use client";

import { Check } from "lucide-react";
import { useTranslations } from "next-intl";
import { useId } from "react";

import {
  HEX_COLOR_LENGTH,
  isHexColor,
  normalizeHexColor,
  pickStageForeground,
  safeStageColor,
  stageColorLowContrastThemes,
  stageColorStyle,
} from "@/lib/stage-color";
import {
  STAGE_COLOR_SWATCHES,
  STAGE_DEFAULT_COLORS,
  THEME_PAGE_BACKGROUNDS,
} from "@/lib/tokens";
import { cn } from "@/lib/utils";

import { Input } from "./input";

interface ColorPickerProps {
  /** The hex triplet held by the form. Invalid input stays here until fixed. */
  value: string;
  onChange: (value: string) => void;
  invalid?: boolean;
  id?: string;
  disabled?: boolean;
  "aria-describedby"?: string;
}

const THEMES = ["light", "dark"] as const;

/**
 * Stage colour control: a row of reviewed base colours, a custom colour with a
 * native picker and a hex field kept in sync, and a preview on both themes so
 * the owner sees the colour where the theme differs.
 */
export function ColorPicker({
  value,
  onChange,
  invalid,
  id,
  disabled,
  "aria-describedby": describedBy,
}: ColorPickerProps) {
  const t = useTranslations("settings.pipeline");
  const baseColorsLabelId = useId();
  const validColor = isHexColor(value);
  const safeColor = safeStageColor(value);
  const lowContrastThemes = validColor
    ? stageColorLowContrastThemes(value)
    : [];

  function swatchName(key: string): string {
    return t(`colorNames.${key}` as Parameters<typeof t>[0]);
  }

  return (
    <div className="space-y-2.5">
      <div>
        <span
          id={baseColorsLabelId}
          className="text-[11px] text-muted-foreground"
        >
          {t("baseColors")}
        </span>
        <div
          role="group"
          aria-labelledby={baseColorsLabelId}
          className="mt-1.5 grid grid-cols-6 justify-items-start gap-2"
        >
          {STAGE_COLOR_SWATCHES.map((swatch) => {
            const selected = validColor && safeColor === swatch.value;
            const name = swatchName(swatch.key);

            return (
              <button
                key={swatch.key}
                type="button"
                aria-label={name}
                aria-pressed={selected}
                title={name}
                disabled={disabled}
                onClick={() => {
                  if (!selected) onChange(swatch.value);
                }}
                className={cn(
                  "relative inline-flex h-8 w-8 items-center justify-center rounded-full border transition-transform after:absolute after:-inset-1 after:content-[''] focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring disabled:cursor-not-allowed disabled:opacity-50 motion-reduce:transition-none",
                  selected
                    ? "border-foreground ring-2 ring-ring ring-offset-2 ring-offset-card"
                    : "border-border/70 hover:scale-105",
                )}
                style={stageColorStyle(swatch.value)}
              >
                {selected ? (
                  <Check
                    className="h-4 w-4"
                    aria-hidden="true"
                    style={{ color: pickStageForeground(swatch.value) }}
                  />
                ) : null}
              </button>
            );
          })}
        </div>
      </div>

      <div className="flex items-center gap-2">
        <input
          type="color"
          aria-label={t("customColor")}
          title={t("customColor")}
          value={safeColor}
          disabled={disabled}
          onChange={(event) => onChange(event.target.value)}
          className="h-[var(--control-height)] w-12 shrink-0 cursor-pointer rounded-[var(--radius-control)] border border-input bg-background p-0.5 focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring disabled:cursor-not-allowed disabled:opacity-50"
        />
        <Input
          id={id}
          aria-label={id ? undefined : t("hexValue")}
          aria-describedby={describedBy}
          value={value}
          disabled={disabled}
          invalid={invalid}
          maxLength={HEX_COLOR_LENGTH}
          spellCheck={false}
          autoComplete="off"
          placeholder={STAGE_DEFAULT_COLORS[STAGE_DEFAULT_COLORS.length - 1]}
          onChange={(event) => onChange(event.target.value)}
          onBlur={() => {
            const normalized = normalizeHexColor(value);
            if (normalized !== value) onChange(normalized);
          }}
          className="font-mono"
        />
      </div>

      <div className="grid grid-cols-2 gap-2">
        {THEMES.map((theme) => (
          <div key={theme} className="space-y-1.5">
            <span className="text-[11px] text-muted-foreground">
              {theme === "light" ? t("previewLight") : t("previewDark")}
            </span>
            {/* The stage colour is stored once, so each preview pins the page
                background of its theme instead of following the app theme. */}
            <div
              className="rounded-[var(--radius-control)] border border-border p-2"
              style={{ backgroundColor: THEME_PAGE_BACKGROUNDS[theme] }}
            >
              <span
                className={cn(
                  "inline-flex max-w-full items-center truncate rounded-[var(--radius-control)] border px-2 py-1 text-xs font-medium",
                  validColor
                    ? "border-border/50"
                    : "border-dashed border-border text-muted-foreground",
                )}
                style={
                  validColor
                    ? {
                        backgroundColor: safeColor,
                        color: pickStageForeground(safeColor),
                      }
                    : undefined
                }
              >
                {t("previewStage")}
              </span>
            </div>
          </div>
        ))}
      </div>

      {lowContrastThemes.length > 0 ? (
        <p className="text-[11px] font-medium text-warning" role="status">
          {lowContrastThemes
            .map((theme) =>
              theme === "light" ? t("lowContrastLight") : t("lowContrastDark"),
            )
            .join(" ")}
        </p>
      ) : null}
    </div>
  );
}
