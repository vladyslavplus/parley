"use client";

import { Check, Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";

import { useTaskMutations } from "@/features/tasks/hooks/use-task-mutations";
import type { Task } from "@/features/tasks/types";
import { cn } from "@/lib/utils";

interface Props {
  task: Task;
  disabled?: boolean;
  className?: string;
  onStatusChange?: (updatedTask: Task) => void;
}

export function TaskStatusCheckbox({
  task,
  disabled = false,
  className,
  onStatusChange,
}: Props) {
  const t = useTranslations("crm.tasks");
  const { completeTask, reopenTask } = useTaskMutations();
  const [loading, setLoading] = useState(false);

  async function handleToggle(e: React.MouseEvent) {
    e.stopPropagation();
    if (disabled || loading) return;

    setLoading(true);
    try {
      if (task.isCompleted) {
        const updated = await reopenTask(task.id, task.version);
        onStatusChange?.(updated);
      } else {
        const updated = await completeTask(task.id, task.version);
        onStatusChange?.(updated);
      }
    } catch {
      // Mutation hook shows toast
    } finally {
      setLoading(false);
    }
  }

  const label = task.isCompleted
    ? t("reopenTaskAria", { title: task.title })
    : t("completeTaskAria", { title: task.title });

  return (
    <button
      type="button"
      role="checkbox"
      aria-checked={task.isCompleted}
      aria-label={label}
      disabled={disabled || loading}
      onClick={handleToggle}
      className={cn(
        "relative flex h-4 w-4 shrink-0 items-center justify-center rounded-xs border transition-colors focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring",
        task.isCompleted
          ? "border-primary bg-primary text-primary-foreground"
          : "border-border bg-input/40 hover:border-ring/80",
        disabled && "cursor-not-allowed opacity-50",
        className,
      )}
    >
      {loading ? (
        <Loader2 className="h-2.5 w-2.5 animate-spin" />
      ) : task.isCompleted ? (
        <Check className="h-3 w-3 stroke-[2.5]" />
      ) : null}
    </button>
  );
}
