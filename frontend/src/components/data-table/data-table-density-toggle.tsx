"use client";

"use no memo"; // Opt out of React Compiler caching for TanStack Table instance mutations

import { SlidersHorizontal } from "lucide-react";
import { useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import { useDataTableContext } from "./data-table-context";

export function DataTableDensityToggle() {
  const { density, setDensity } = useDataTableContext();
  const t = useTranslations("table");

  return (
    <Button
      variant="secondary"
      onClick={() =>
        setDensity(density === "compact" ? "comfortable" : "compact")
      }
      className="h-8 px-2.5 text-xs gap-1.5"
      title={t("density.label")}
    >
      <SlidersHorizontal className="h-3.5 w-3.5 text-muted-foreground" />
      <span className="capitalize">
        {density === "compact"
          ? t("density.compact")
          : t("density.comfortable")}
      </span>
    </Button>
  );
}
