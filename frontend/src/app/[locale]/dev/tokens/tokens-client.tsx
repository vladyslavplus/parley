"use client";

import { ArrowLeft } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";

import { LocaleToggle } from "@/components/layout/locale-toggle";
import { ThemeToggle } from "@/components/layout/theme-toggle";
import { Button } from "@/components/ui/button";
import { ROUTES } from "@/constants/routes";
import { Link } from "@/i18n/navigation";
import {
  showErrorToast,
  showInfoToast,
  showSuccessToast,
  showWarningToast,
  useUndoToast,
} from "@/lib/toasts";
import {
  COLOR_TOKENS,
  computeContrastRatio,
  MOTION_TOKENS,
  RADIUS_TOKENS,
  STAGE_INDICES,
  TAG_INDICES,
} from "@/lib/tokens";

interface ComputedColorState {
  values: Record<string, string>;
  contrasts: Record<string, string>;
}

export function TokensClient() {
  const t = useTranslations("dev.tokens");
  const undoToast = useUndoToast();
  const [computed, setComputed] = useState<ComputedColorState>({
    values: {},
    contrasts: {},
  });

  useEffect(() => {
    function updateComputedTokens() {
      const style = window.getComputedStyle(document.documentElement);
      const values: Record<string, string> = {};
      const contrasts: Record<string, string> = {};

      for (const token of COLOR_TOKENS) {
        const val = style.getPropertyValue(token.cssVar).trim();
        values[token.cssVar] = val;
        if (token.fgVar) {
          const fg = style.getPropertyValue(token.fgVar).trim();
          if (val && fg) {
            const ratio = computeContrastRatio(val, fg);
            contrasts[token.cssVar] = `${ratio.toFixed(2)}:1`;
          }
        }
      }

      for (const stage of STAGE_INDICES) {
        const bg = style.getPropertyValue(`--stage-${stage}`).trim();
        const fg = style.getPropertyValue(`--stage-${stage}-foreground`).trim();
        values[`--stage-${stage}`] = bg;
        values[`--stage-${stage}-foreground`] = fg;
        if (bg && fg) {
          contrasts[`--stage-${stage}`] =
            `${computeContrastRatio(bg, fg).toFixed(2)}:1`;
        }
      }

      for (const tag of TAG_INDICES) {
        const bg = style.getPropertyValue(`--tag-${tag}`).trim();
        const fg = style.getPropertyValue(`--tag-${tag}-foreground`).trim();
        values[`--tag-${tag}`] = bg;
        values[`--tag-${tag}-foreground`] = fg;
        if (bg && fg) {
          contrasts[`--tag-${tag}`] =
            `${computeContrastRatio(bg, fg).toFixed(2)}:1`;
        }
      }

      setComputed({ values, contrasts });
    }

    updateComputedTokens();

    const observer = new MutationObserver(() => {
      updateComputedTokens();
    });

    observer.observe(document.documentElement, {
      attributes: true,
      attributeFilter: ["class", "style"],
    });

    return () => observer.disconnect();
  }, []);

  return (
    <div className="mx-auto max-w-6xl px-6 py-10 space-y-12">
      <header className="flex flex-col gap-4 border-b border-border pb-6 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div className="mb-2">
            <Link
              href={ROUTES.app}
              className="inline-flex items-center gap-1.5 text-xs text-muted-foreground hover:text-foreground transition-colors"
            >
              <ArrowLeft className="h-3.5 w-3.5" />
              <span>{t("backToApp")}</span>
            </Link>
          </div>
          <h1 className="text-2xl font-semibold tracking-tight font-display text-foreground">
            {t("title")}
          </h1>
          <p className="mt-1 text-sm text-muted-foreground">
            {t("description")}
          </p>
        </div>
        <div className="flex items-center gap-3">
          <LocaleToggle />
          <ThemeToggle />
        </div>
      </header>

      {/* Semantic Colors */}
      <section className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-foreground">
            {t("sections.colors")}
          </h2>
          <p className="text-xs text-muted-foreground">
            {t("sections.colorsDescription")}
          </p>
        </div>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {COLOR_TOKENS.map((token) => (
            <div
              key={token.cssVar}
              className="rounded-[var(--radius-card)] border border-border bg-card p-4 space-y-2"
            >
              <div className="flex items-center justify-between">
                <span className="font-mono text-xs font-semibold text-foreground">
                  {token.cssVar}
                </span>
                <span className="rounded bg-background px-1.5 py-0.5 text-[10px] text-muted-foreground border border-border">
                  {token.name}
                </span>
              </div>
              <div className="flex gap-2 text-xs">
                <div className="flex-1 rounded-[var(--radius-control)] border border-border p-2 bg-background">
                  <div className="text-[10px] text-muted-foreground">
                    {t("labels.runtimeValue")}
                  </div>
                  <div className="font-mono text-foreground font-medium">
                    {computed.values[token.cssVar] || t("labels.computing")}
                  </div>
                </div>
              </div>
              <div className="text-xs text-muted-foreground">
                {token.description}
              </div>
              {computed.contrasts[token.cssVar] && (
                <div className="text-[11px] font-medium text-ring">
                  {t("labels.contrast")}: {computed.contrasts[token.cssVar]}
                </div>
              )}
            </div>
          ))}
        </div>
      </section>

      {/* Pipeline Stage Ramp */}
      <section className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-foreground">
            {t("sections.stages")}
          </h2>
          <p className="text-xs text-muted-foreground">
            {t("sections.stagesDescription")}
          </p>
        </div>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-5">
          {STAGE_INDICES.map((stage) => (
            <div
              key={stage}
              className="rounded-[var(--radius-card)] border border-border p-4 space-y-2 text-center"
              style={{
                backgroundColor: `var(--stage-${stage})`,
                color: `var(--stage-${stage}-foreground)`,
              }}
            >
              <div className="font-semibold text-sm">Stage {stage}</div>
              <div className="text-xs font-mono opacity-90">
                {computed.values[`--stage-${stage}`]}
              </div>
              {computed.contrasts[`--stage-${stage}`] && (
                <div className="text-[10px] font-mono opacity-75">
                  {computed.contrasts[`--stage-${stage}`]}
                </div>
              )}
            </div>
          ))}
        </div>
      </section>

      {/* Pastel Tag Fills */}
      <section className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-foreground">
            {t("sections.tags")}
          </h2>
          <p className="text-xs text-muted-foreground">
            {t("sections.tagsDescription")}
          </p>
        </div>
        <div className="flex flex-wrap gap-3">
          {TAG_INDICES.map((tag) => (
            <div
              key={tag}
              className="inline-flex items-center gap-2 rounded px-3 py-1.5 text-xs font-medium border border-border"
              style={{
                backgroundColor: `var(--tag-${tag})`,
                color: `var(--tag-${tag}-foreground)`,
              }}
            >
              <span>
                {t("labels.tagPrefix")} #{tag}
              </span>
              <span className="font-mono text-[10px] opacity-75">
                {computed.contrasts[`--tag-${tag}`] || ">11:1"}
              </span>
            </div>
          ))}
        </div>
      </section>

      {/* Typography Scale */}
      <section className="space-y-6">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-foreground">
            {t("sections.typography")}
          </h2>
          <p className="text-xs text-muted-foreground">
            {t("sections.typographyDescription")}
          </p>
        </div>

        <div className="space-y-4 rounded-[var(--radius-card)] border border-border bg-card p-6">
          <div className="space-y-1">
            <span className="font-mono text-xs text-muted-foreground">
              {t("labels.displayHeading")}
            </span>
            <div className="text-3xl font-display font-medium text-foreground">
              {t("labels.displayHeadingSample")}
            </div>
          </div>

          <div className="space-y-1">
            <span className="font-mono text-xs text-muted-foreground">
              {t("labels.heading1")}
            </span>
            <div className="text-2xl font-semibold text-foreground">
              {t("labels.heading1Sample")}
            </div>
          </div>

          <div className="space-y-1">
            <span className="font-mono text-xs text-muted-foreground">
              {t("labels.heading2")}
            </span>
            <div className="text-lg font-semibold text-foreground">
              {t("labels.heading2Sample")}
            </div>
          </div>

          <div className="space-y-1">
            <span className="font-mono text-xs text-muted-foreground">
              {t("labels.bodyText")}
            </span>
            <div className="text-sm text-foreground">
              {t("labels.bodyTextSample")}
            </div>
          </div>

          <div className="space-y-2 border-t border-border pt-4">
            <span className="font-mono text-xs text-muted-foreground">
              {t("labels.tabularTest")}
            </span>
            <div className="space-y-1 font-mono text-sm tabular-nums text-foreground">
              <div>111,111.11 UAH | 2026-11-08 | ID: 1111-1111</div>
              <div>888,888.88 UAH | 2026-11-08 | ID: 8888-8888</div>
            </div>
          </div>
        </div>
      </section>

      {/* Radius Tokens */}
      <section className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-foreground">
            {t("sections.radius")}
          </h2>
          <p className="text-xs text-muted-foreground">
            {t("sections.radiusDescription")}
          </p>
        </div>
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
          {RADIUS_TOKENS.map((r) => (
            <div
              key={r.name}
              className="border border-input bg-card p-4 space-y-2 text-center"
              style={{ borderRadius: r.value }}
            >
              <div className="font-mono text-xs font-semibold text-foreground">
                --radius-{r.name}
              </div>
              <div className="text-xs text-muted-foreground">{r.value}</div>
              <div className="text-[11px] text-muted-foreground">{r.use}</div>
            </div>
          ))}
        </div>
      </section>

      {/* Motion Tokens */}
      <section className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-foreground">
            {t("sections.motion")}
          </h2>
          <p className="text-xs text-muted-foreground">
            {t("sections.motionDescription")}
          </p>
        </div>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          {MOTION_TOKENS.map((m) => (
            <div
              key={m.name}
              className="rounded-[var(--radius-card)] border border-border bg-card p-4 space-y-2"
            >
              <div className="font-mono text-xs font-semibold text-foreground">
                --duration-{m.name}
              </div>
              <div className="text-xs text-ring">{m.duration}</div>
              <div className="font-mono text-[11px] text-muted-foreground">
                {m.ease}
              </div>
              <div className="text-xs text-muted-foreground">{m.use}</div>
            </div>
          ))}
        </div>
      </section>

      {/* Toast Notifications Showcase */}
      <section className="space-y-4">
        <div>
          <h2 className="text-lg font-semibold tracking-tight text-foreground">
            {t("sections.toasts")}
          </h2>
          <p className="text-xs text-muted-foreground">
            {t("sections.toastsDescription")}
          </p>
        </div>
        <div className="flex flex-wrap gap-3">
          <Button
            variant="secondary"
            onClick={() => showSuccessToast(t("toasts.successMessage"))}
          >
            {t("toasts.triggerSuccess")}
          </Button>
          <Button
            variant="secondary"
            onClick={() => showErrorToast(t("toasts.errorMessage"))}
          >
            {t("toasts.triggerError")}
          </Button>
          <Button
            variant="secondary"
            onClick={() => showInfoToast(t("toasts.infoMessage"))}
          >
            {t("toasts.triggerInfo")}
          </Button>
          <Button
            variant="secondary"
            onClick={() => showWarningToast(t("toasts.warningMessage"))}
          >
            {t("toasts.triggerWarning")}
          </Button>
          <Button
            variant="secondary"
            onClick={() =>
              undoToast({
                message: t("toasts.undoMessage"),
                onUndo: () => {},
              })
            }
          >
            {t("toasts.triggerUndo")}
          </Button>
        </div>
      </section>
    </div>
  );
}
