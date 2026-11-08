"use client";

import { AlertTriangle, RotateCcw } from "lucide-react";
import { useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";

export default function AppError({
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  const t = useTranslations("errors.boundary");

  return (
    <div
      role="alert"
      className="flex min-h-[50vh] flex-col items-center justify-center p-6 text-center"
    >
      <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10 text-destructive">
        <AlertTriangle className="h-6 w-6" aria-hidden="true" />
      </div>
      <h2 className="text-lg font-semibold text-foreground">{t("title")}</h2>
      <p className="mt-1 max-w-md text-sm text-muted-foreground">
        {t("description")}
      </p>
      <Button
        variant="secondary"
        onClick={reset}
        className="mt-6 gap-2 text-sm"
      >
        <RotateCcw className="h-4 w-4" />
        {t("retry")}
      </Button>
    </div>
  );
}
