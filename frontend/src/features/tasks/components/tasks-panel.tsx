"use client";

import { Calendar, Plus, Trash2 } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { TableProgressBar } from "@/components/data-table/table-progress-bar";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { PERMISSIONS } from "@/constants/permissions";
import { TaskModal } from "@/features/tasks/components/task-modal";
import { TaskStatusCheckbox } from "@/features/tasks/components/task-status-checkbox";
import { useTaskMutations } from "@/features/tasks/hooks/use-task-mutations";
import { useSubjectTasks } from "@/features/tasks/hooks/use-tasks";
import type { Task } from "@/features/tasks/types";
import { Can } from "@/features/session/components/can";
import { useMe } from "@/lib/api/hooks/use-api";
import { formatDateTime, formatRelativeAge } from "@/lib/formatters";
import { displayTimeZone, isTaskOverdue } from "@/lib/time-zones";
import { showSuccessToast } from "@/lib/toasts";
import { cn } from "@/lib/utils";

interface Props {
  subject: {
    contactId?: string;
    contactName?: string | null;
    dealId?: string;
    dealTitle?: string | null;
    companyId?: string;
    companyName?: string | null;
  };
}

export function TasksPanel({ subject }: Props) {
  const t = useTranslations("crm.tasks");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const { data: me } = useMe();
  const timeZone = displayTimeZone(me);
  const { deleteTask } = useTaskMutations();

  const [modalOpen, setModalOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<Task | null>(null);
  const [deletingTask, setDeletingTask] = useState<Task | null>(null);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);

  const { data: tasksPage, isLoading, isFetching } = useSubjectTasks(subject);
  const tasks = tasksPage?.items ?? [];

  async function handleDeleteConfirm() {
    if (!deletingTask) return;
    await deleteTask(deletingTask.id);
    showSuccessToast(t("taskDeleted"));
    setDeleteConfirmOpen(false);
    setDeletingTask(null);
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-foreground">
          {t("panel.title")}
        </h3>

        <Can permission={PERMISSIONS.tasksWrite}>
          <Button
            type="button"
            variant="secondary"
            onClick={() => {
              setEditingTask(null);
              setModalOpen(true);
            }}
            className="gap-1.5 px-2.5 text-xs"
          >
            <Plus className="h-3.5 w-3.5" />
            <span>{t("newTask")}</span>
          </Button>
        </Can>
      </div>

      <TableProgressBar active={isFetching} />

      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 3 }).map((_, i) => (
            <div
              key={i}
              className="h-12 w-full animate-pulse rounded-[var(--radius-control)] bg-muted/40"
            />
          ))}
        </div>
      ) : tasks.length === 0 ? (
        <div className="flex h-32 items-center justify-center rounded-[var(--radius-card)] border border-dashed border-border p-6 text-center text-xs text-muted-foreground">
          {t("panel.empty")}
        </div>
      ) : (
        <div className="divide-y divide-border rounded-[var(--radius-card)] border border-border bg-card">
          {tasks.map((task) => {
            const isOverdue = isTaskOverdue(task.dueAt, task.isCompleted);

            return (
              <div
                key={task.id}
                className="flex items-center justify-between gap-3 p-3 transition-colors hover:bg-muted/30"
              >
                <div className="flex items-center gap-3 flex-1 min-w-0">
                  <TaskStatusCheckbox task={task} />
                  <div className="min-w-0 flex-1 space-y-0.5">
                    <button
                      type="button"
                      onClick={() => {
                        setEditingTask(task);
                        setModalOpen(true);
                      }}
                      className={cn(
                        "text-left text-xs font-medium hover:underline focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring truncate block max-w-full",
                        task.isCompleted
                          ? "line-through text-muted-foreground"
                          : "text-foreground",
                      )}
                    >
                      {task.title}
                    </button>
                    {task.dueAt && (
                      <div
                        className={cn(
                          "flex items-center gap-1.5 text-[11px] tabular-nums",
                          isOverdue
                            ? "font-semibold text-destructive"
                            : "text-muted-foreground",
                        )}
                        title={formatDateTime(task.dueAt, locale, timeZone)}
                      >
                        <Calendar className="h-3 w-3 shrink-0" />
                        <span>{formatRelativeAge(task.dueAt, locale)}</span>
                        {isOverdue && (
                          <span className="rounded-xs bg-destructive/15 px-1 py-0.2 text-[9px] font-bold uppercase text-destructive">
                            {t("overdueBadge")}
                          </span>
                        )}
                      </div>
                    )}
                  </div>
                </div>

                <div className="flex items-center gap-2 shrink-0">
                  {task.assigneeDisplayName && (
                    <span className="text-[11px] text-muted-foreground truncate max-w-[120px]">
                      {task.assigneeDisplayName}
                    </span>
                  )}
                  <Can permission={PERMISSIONS.tasksDelete}>
                    <button
                      type="button"
                      onClick={() => {
                        setDeletingTask(task);
                        setDeleteConfirmOpen(true);
                      }}
                      aria-label={t("deleteTask")}
                      className="rounded-xs p-1 text-muted-foreground hover:bg-destructive/10 hover:text-destructive focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </Can>
                </div>
              </div>
            );
          })}
        </div>
      )}

      <TaskModal
        open={modalOpen}
        onOpenChange={setModalOpen}
        task={editingTask}
        initialSubject={subject}
      />

      <ConfirmDialog
        open={deleteConfirmOpen}
        onOpenChange={setDeleteConfirmOpen}
        title={t("deleteConfirmTitle")}
        description={t("deleteConfirmDescription", {
          title: deletingTask?.title ?? "",
        })}
        confirmLabel={t("deleteTask")}
        cancelLabel={tCommon("cancel")}
        onConfirm={handleDeleteConfirm}
        destructive
      />
    </div>
  );
}
