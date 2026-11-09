import type { Ref, ReactNode } from "react";

import { cn } from "@/lib/utils";

export type AlertTone = "error" | "info" | "success" | "warning";

const TONE_CLASSES: Record<AlertTone, string> = {
  error: "border-destructive/40 bg-destructive/10 text-destructive",
  info: "border-info/40 bg-info/10 text-info",
  success: "border-success/40 bg-success/10 text-success",
  warning: "border-warning/40 bg-warning/10 text-warning",
};

export function Alert({
  children,
  tone = "error",
  ref,
}: {
  children: ReactNode;
  tone?: AlertTone;
  ref?: Ref<HTMLDivElement>;
}) {
  return (
    <div
      ref={ref}
      role={tone === "error" ? "alert" : "status"}
      tabIndex={-1}
      className={cn(
        "rounded-md border px-3 py-2 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring",
        TONE_CLASSES[tone],
      )}
    >
      {children}
    </div>
  );
}
