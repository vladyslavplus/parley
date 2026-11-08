"use client";

import { GlobalStatusPage } from "@/components/ui/global-status-page";

export default function GlobalError({
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return <GlobalStatusPage status="500" onRetry={reset} />;
}
