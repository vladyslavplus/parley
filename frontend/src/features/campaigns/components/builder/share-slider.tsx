"use client";

import { useId } from "react";

import { bpsToPercent } from "@/features/campaigns/lib/holdout-math";

interface Props {
  label: string;
  valueBps: number;
  minBps: number;
  maxBps: number;
  stepBps: number;
  onChange: (bps: number) => void;
}

// Percent slider over a basis-point value; arrow keys move one step.
export function ShareSlider({
  label,
  valueBps,
  minBps,
  maxBps,
  stepBps,
  onChange,
}: Props) {
  const id = useId();
  return (
    <div className="space-y-2">
      <div className="flex items-baseline justify-between gap-3">
        <label htmlFor={id} className="text-xs font-medium text-foreground">
          {label}
        </label>
        <output
          htmlFor={id}
          className="text-sm font-semibold tabular-nums text-foreground"
        >
          {bpsToPercent(valueBps)}%
        </output>
      </div>
      <input
        id={id}
        type="range"
        min={minBps}
        max={maxBps}
        step={stepBps}
        value={valueBps}
        onChange={(event) => onChange(Number(event.target.value))}
        className="h-[var(--control-height)] w-full cursor-pointer accent-[var(--ring)] focus-visible:outline-2 focus-visible:outline-ring"
      />
      <div className="flex justify-between text-[10px] tabular-nums text-muted-foreground">
        <span>{bpsToPercent(minBps)}%</span>
        <span>{bpsToPercent(maxBps)}%</span>
      </div>
    </div>
  );
}
