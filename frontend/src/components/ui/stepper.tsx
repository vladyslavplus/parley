"use client";

import { Check, CloudAlert, CloudCheck, Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";
import type { ReactNode } from "react";

import { Button } from "./button";
import { cn } from "@/lib/utils";

export interface StepItem {
  id: string;
  title: string;
  description?: string;
}

export type AutosaveStatus = "saved" | "saving" | "idle" | "error";

export interface StepperProps {
  steps: StepItem[];
  currentStepIndex: number;
  onStepChange: (newIndex: number) => void;
  autosaveStatus?: AutosaveStatus;
  children: ReactNode;
  onComplete?: () => void;
  isSubmitting?: boolean;
  // Blocks "Next" while the current step is incomplete.
  canAdvance?: boolean;
  // Label and availability of the last-step action; hidden when null.
  finishLabel?: string | null;
  finishDisabled?: boolean;
  // Highest step a header button may jump to; defaults to the current step.
  maxStepIndex?: number;
}

export function Stepper({
  steps,
  currentStepIndex,
  onStepChange,
  autosaveStatus = "saved",
  children,
  onComplete,
  isSubmitting = false,
  canAdvance = true,
  finishLabel,
  finishDisabled = false,
  maxStepIndex,
}: StepperProps) {
  const t = useTranslations("forms.stepper");
  const tCommon = useTranslations("common");

  const isFirstStep = currentStepIndex === 0;
  const isLastStep = currentStepIndex === steps.length - 1;

  return (
    <div className="space-y-6">
      {/* Stepper Header + Autosave status */}
      <div className="flex flex-col gap-4 border-b border-border pb-5 sm:flex-row sm:items-center sm:justify-between">
        <nav
          aria-label={tCommon("steps")}
          className="flex flex-wrap items-center gap-3"
        >
          {steps.map((step, idx) => {
            const isCompleted = idx < currentStepIndex;
            const isCurrent = idx === currentStepIndex;

            return (
              <div key={step.id} className="flex items-center gap-2">
                <button
                  type="button"
                  disabled={idx > Math.max(currentStepIndex, maxStepIndex ?? 0)}
                  onClick={() => onStepChange(idx)}
                  className={cn(
                    "flex items-center gap-2 rounded-[var(--radius-control)] px-2.5 py-1 text-xs font-medium transition-colors",
                    isCurrent && "bg-muted text-foreground font-semibold",
                    !isCurrent &&
                      idx <= Math.max(currentStepIndex, maxStepIndex ?? 0) &&
                      "text-muted-foreground hover:text-foreground cursor-pointer",
                    idx > Math.max(currentStepIndex, maxStepIndex ?? 0) &&
                      "text-muted-foreground opacity-50",
                  )}
                  aria-current={isCurrent ? "step" : undefined}
                >
                  <span
                    className={cn(
                      "flex h-4 w-4 items-center justify-center rounded-full text-[10px]",
                      isCompleted ? "bg-success text-success-foreground" : "",
                    )}
                  >
                    {isCompleted ? <Check className="h-3 w-3" /> : idx + 1}
                  </span>
                  <span>{step.title}</span>
                </button>
                {idx < steps.length - 1 && (
                  <span className="text-border" aria-hidden="true">
                    /
                  </span>
                )}
              </div>
            );
          })}
        </nav>

        {/* Autosave status badge */}
        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
          {autosaveStatus === "saving" ? (
            <>
              <Loader2 className="h-3.5 w-3.5 animate-spin text-info" />
              <span>{t("autosave.saving")}</span>
            </>
          ) : autosaveStatus === "error" ? (
            <>
              <CloudAlert className="h-3.5 w-3.5 text-destructive" />
              <span>{t("autosave.error")}</span>
            </>
          ) : (
            <>
              <CloudCheck className="h-3.5 w-3.5 text-success" />
              <span>{t("autosave.saved")}</span>
            </>
          )}
        </div>
      </div>

      {/* Step Content */}
      <div className="min-h-[240px]">{children}</div>

      {/* Footer Navigation */}
      <div className="flex items-center justify-between border-t border-border pt-4">
        <Button
          variant="secondary"
          disabled={isFirstStep || isSubmitting}
          onClick={() => onStepChange(currentStepIndex - 1)}
        >
          {t("previous")}
        </Button>

        {isLastStep ? (
          finishLabel === null ? (
            <span />
          ) : (
            <Button
              variant="primary"
              disabled={isSubmitting || finishDisabled}
              onClick={onComplete}
            >
              {finishLabel ?? t("finish")}
            </Button>
          )
        ) : (
          <Button
            variant="primary"
            disabled={!canAdvance}
            onClick={() => onStepChange(currentStepIndex + 1)}
          >
            {t("next")}
          </Button>
        )}
      </div>
    </div>
  );
}
