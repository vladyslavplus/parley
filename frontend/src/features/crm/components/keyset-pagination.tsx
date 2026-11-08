"use client";

import { ArrowLeft, ArrowRight } from "lucide-react";
import { useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";

interface Props {
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  onPreviousPage: () => void;
  onNextPage: () => void;
}

export function KeysetPagination({
  hasPreviousPage,
  hasNextPage,
  onPreviousPage,
  onNextPage,
}: Props) {
  const t = useTranslations("crm.pagination");

  return (
    <div className="flex items-center gap-2">
      <Button
        variant="secondary"
        onClick={onPreviousPage}
        disabled={!hasPreviousPage}
        className="h-8 gap-1 text-xs px-2.5"
      >
        <ArrowLeft className="h-3.5 w-3.5" />
        <span>{t("previous")}</span>
      </Button>
      <Button
        variant="secondary"
        onClick={onNextPage}
        disabled={!hasNextPage}
        className="h-8 gap-1 text-xs px-2.5"
      >
        <span>{t("next")}</span>
        <ArrowRight className="h-3.5 w-3.5" />
      </Button>
    </div>
  );
}
