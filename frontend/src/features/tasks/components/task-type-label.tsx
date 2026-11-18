"use client";

import { useTranslations } from "next-intl";

import {
  TASK_TYPE_ICONS,
  TASK_TYPE_LABEL_KEYS,
  taskTypeOf,
} from "@/features/tasks/lib/task-types";
import { cn } from "@/lib/utils";

// The type of a task as an icon and its name.
export function TaskTypeLabel({
  type,
  iconOnly = false,
  className,
}: {
  type: string;
  /** The icon alone, named for assistive technology; the table column shows both. */
  iconOnly?: boolean;
  className?: string;
}) {
  const t = useTranslations("crm.tasks");
  const code = taskTypeOf(type);
  const Icon = TASK_TYPE_ICONS[code];
  const label = t(TASK_TYPE_LABEL_KEYS[code]);
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 text-xs text-foreground",
        className,
      )}
      title={iconOnly ? label : undefined}
    >
      <Icon
        className="h-3.5 w-3.5 shrink-0 text-muted-foreground"
        aria-hidden={iconOnly ? undefined : true}
        aria-label={iconOnly ? label : undefined}
      />
      {iconOnly ? null : <span>{label}</span>}
    </span>
  );
}
