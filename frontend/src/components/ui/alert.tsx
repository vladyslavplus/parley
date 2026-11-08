import type { Ref } from "react";

import { cn } from "@/lib/utils";

export function Alert({
  children,
  tone = "error",
  ref,
}: {
  children: React.ReactNode;
  tone?: "error" | "info";
  ref?: Ref<HTMLDivElement>;
}) {
  return (
    <div
      ref={ref}
      role={tone === "error" ? "alert" : "status"}
      tabIndex={-1}
      className={cn(
        "rounded-md border px-3 py-2 text-sm focus-visible:outline-2 focus-visible:outline-offset-1 focus-visible:outline-neutral-900",
        tone === "error"
          ? "border-red-300 bg-red-50 text-red-900"
          : "border-neutral-300 bg-neutral-50 text-neutral-900",
      )}
    >
      {children}
    </div>
  );
}
