"use client";

import { ArrowLeft, ArrowRight } from "lucide-react";
import { useTranslations } from "next-intl";

import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { LIMITS } from "@/constants/limits";

interface Props {
  hasPreviousPage: boolean;
  hasNextPage: boolean;
  onPreviousPage: () => void;
  onNextPage: () => void;
  pageSize?: number;
  onPageSizeChange?: (size: number) => void;
  itemCount?: number;
}

export function KeysetPagination({
  hasPreviousPage,
  hasNextPage,
  onPreviousPage,
  onNextPage,
  pageSize = LIMITS.defaultTablePageSize,
  onPageSizeChange,
  itemCount,
}: Props) {
  const tCrm = useTranslations("crm");
  const tTable = useTranslations("table");

  return (
    <div className="flex flex-wrap items-center justify-between gap-4 w-full">
      <div className="flex items-center gap-3">
        {onPageSizeChange && (
          <div className="flex items-center gap-2">
            <span className="text-xs text-muted-foreground whitespace-nowrap">
              {tTable("pagination.rowsPerPage")}
            </span>
            <Select
              value={String(pageSize)}
              onValueChange={(val) => onPageSizeChange(Number(val))}
            >
              <SelectTrigger className="h-8 w-16 text-xs bg-background">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {LIMITS.tablePageSizes.map((size) => (
                  <SelectItem key={size} value={String(size)}>
                    {size}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        )}

        {itemCount !== undefined && (
          <span className="text-xs text-muted-foreground tabular-nums">
            {tCrm("table.pageCount", { count: itemCount })}
          </span>
        )}
      </div>

      <div className="flex items-center gap-2 ml-auto">
        <Button
          variant="secondary"
          onClick={onPreviousPage}
          disabled={!hasPreviousPage}
          className="h-8 gap-1 text-xs px-2.5"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          <span>{tCrm("pagination.previous")}</span>
        </Button>
        <Button
          variant="secondary"
          onClick={onNextPage}
          disabled={!hasNextPage}
          className="h-8 gap-1 text-xs px-2.5"
        >
          <span>{tCrm("pagination.next")}</span>
          <ArrowRight className="h-3.5 w-3.5" />
        </Button>
      </div>
    </div>
  );
}
