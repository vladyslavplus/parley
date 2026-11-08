import type { Ref, ReactNode } from "react";

import { cn } from "@/lib/utils";

export function Alert({
  children,
  tone = "error",
  ref,
}: {
  children: ReactNode;
  tone?: "error" | "info";
  ref?: Ref<HTMLDivElement>;
}) {
  return (
    <div
      ref={ref}
      role={tone === "error" ? "alert" : "status"}
      tabIndex={-1}
      className={cn(
        "rounded-md border px-3 py-2 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-ring",
        tone === "error"
          ? "border-destructive/40 bg-destructive/10 text-destructive dark:text-destructive-foreground"
          : "border-info/40 bg-info/10 text-info dark:text-info-foreground",
      )}
    >
      {children}
    </div>
  );
}
