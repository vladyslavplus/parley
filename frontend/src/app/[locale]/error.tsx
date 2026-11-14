"use client";

import { RotateCcw } from "lucide-react";
import { useTranslations } from "next-intl";

import { LogoMark } from "@/components/brand/logo-mark";
import { Button } from "@/components/ui/button";

export default function LocaleError({
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
      <LogoMark size={32} className="mb-4" />
      <h2 className="font-display text-3xl font-medium tracking-[-0.02em] text-balance text-foreground">
        {t("title")}
      </h2>
      <p className="mt-3 max-w-md text-sm leading-relaxed text-muted-foreground">
        {t("description")}
      </p>
      <Button
        variant="secondary"
        onClick={reset}
        className="mt-6 gap-2 text-sm"
      >
        <RotateCcw className="h-4 w-4" aria-hidden="true" />
        {t("retry")}
      </Button>
    </div>
  );
}
