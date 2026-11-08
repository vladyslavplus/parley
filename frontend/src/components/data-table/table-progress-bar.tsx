"use client";

import { useTranslations } from "next-intl";
import { useEffect, useState } from "react";

import { LIMITS } from "@/constants/limits";
import { cn } from "@/lib/utils";

interface Props {
  active: boolean;
  delayMs?: number;
  className?: string;
  label?: string;
}

export function TableProgressBar({
  active,
  delayMs = LIMITS.tableProgressDelayMs,
  className,
  label,
}: Props) {
  const t = useTranslations("common");
  const [prevActive, setPrevActive] = useState(active);
  const [delayedActive, setDelayedActive] = useState(false);

  if (active !== prevActive) {
    setPrevActive(active);
    setDelayedActive(false);
  }

  useEffect(() => {
    if (!active) {
      return;
    }

    const timer = setTimeout(() => {
      setDelayedActive(true);
    }, delayMs);

    return () => clearTimeout(timer);
  }, [active, delayMs]);

  if (!active || !delayedActive) return null;

  return (
    <div
      role="progressbar"
      aria-label={label ?? t("loading")}
      className={cn(
        "absolute top-0 left-0 right-0 h-0.5 overflow-hidden z-10 bg-primary/20",
        className,
      )}
    >
      <div className="h-full w-full bg-primary origin-left animate-indeterminate" />
    </div>
  );
}
