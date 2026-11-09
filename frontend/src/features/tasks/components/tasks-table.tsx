"use client";

import type {
  ColumnDef,
  OnChangeFn,
  SortingState,
} from "@tanstack/react-table";
import { Building2, Calendar, Edit2, Kanban, Trash2, User } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { DataTable } from "@/components/data-table/data-table";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { PERMISSIONS } from "@/constants/permissions";
import {
  companyRecordRoute,
  contactRecordRoute,
  dealRecordRoute,
} from "@/constants/routes";
import { TaskModal } from "@/features/tasks/components/task-modal";
import { TaskStatusCheckbox } from "@/features/tasks/components/task-status-checkbox";
import { useTaskMutations } from "@/features/tasks/hooks/use-task-mutations";
import type { Task } from "@/features/tasks/types";
import { Can } from "@/features/session/components/can";
import { Link } from "@/i18n/navigation";
import { useMe } from "@/lib/api/hooks/use-api";
import { formatDateTime, formatRelativeAge } from "@/lib/formatters";
import { displayTimeZone, isTaskOverdue } from "@/lib/time-zones";
import { showSuccessToast } from "@/lib/toasts";
import { cn } from "@/lib/utils";

interface Props {
  tasks: Task[];
  isLoading: boolean;
  isFetching?: boolean;
  sorting?: SortingState;
  onSortingChange?: OnChangeFn<SortingState>;
  paginationSlot?: React.ReactNode;
  onRefresh?: () => void;
}

export function TasksTable({
  tasks,
  isLoading,
  isFetching,
  sorting,
  onSortingChange,
  paginationSlot,
}: Props) {
  const t = useTranslations("crm.tasks");
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const { data: me } = useMe();
  const timeZone = displayTimeZone(me);
  const { deleteTask } = useTaskMutations();

  const [editingTask, setEditingTask] = useState<Task | null>(null);
  const [taskModalOpen, setTaskModalOpen] = useState(false);
  const [deletingTask, setDeletingTask] = useState<Task | null>(null);
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);

  async function handleDeleteConfirm() {
    if (!deletingTask) return;
    await deleteTask(deletingTask.id);
    showSuccessToast(t("taskDeleted"));
    setDeleteConfirmOpen(false);
    setDeletingTask(null);
  }

  const columns: ColumnDef<Task, unknown>[] = [
    {
      id: "status",
      header: "",
      size: 40,
      cell: ({ row }) => <TaskStatusCheckbox task={row.original} />,
    },
    {
      id: "title",
      accessorKey: "title",
      header: t("table.title"),
      enableSorting: true,
      cell: ({ row }) => {
        const task = row.original;
        return (
          <div className="space-y-0.5">
            <button
              type="button"
              onClick={() => {
                setEditingTask(task);
                setTaskModalOpen(true);
              }}
              className={cn(
                "text-left font-medium hover:underline focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring text-xs",
                task.isCompleted
                  ? "line-through text-muted-foreground"
                  : "text-foreground",
              )}
            >
              {task.title}
            </button>
            {task.notes && (
              <p className="line-clamp-1 text-[11px] text-muted-foreground">
                {task.notes}
              </p>
            )}
          </div>
        );
      },
    },
    {
      id: "due",
      accessorKey: "dueAt",
      header: t("table.dueDate"),
      enableSorting: true,
      size: 180,
      cell: ({ row }) => {
        const task = row.original;
        if (!task.dueAt) {
          return <span className="text-muted-foreground text-xs">—</span>;
        }

        const isOverdue = isTaskOverdue(task.dueAt, task.isCompleted);

        return (
          <div
            className={cn(
              "flex items-center gap-1.5 text-xs tabular-nums",
              isOverdue
                ? "font-semibold text-destructive"
                : task.isCompleted
                  ? "text-muted-foreground line-through"
                  : "text-foreground",
            )}
            title={formatDateTime(task.dueAt, locale, timeZone)}
          >
            <Calendar
              className={cn(
                "h-3.5 w-3.5 shrink-0",
                isOverdue ? "text-destructive" : "text-muted-foreground",
              )}
            />
            <span>{formatRelativeAge(task.dueAt, locale)}</span>
            {isOverdue && (
              <span className="rounded-xs bg-destructive/15 px-1 py-0.2 text-[10px] font-bold uppercase tracking-wider text-destructive">
                {t("overdueBadge")}
              </span>
            )}
          </div>
        );
      },
    },
    {
      id: "assignee",
      accessorKey: "assigneeDisplayName",
      header: t("table.assignee"),
      size: 160,
      cell: ({ row }) => {
        const task = row.original;
        const name = task.assigneeDisplayName || task.assigneeEmail || "—";
        return (
          <div className="flex items-center gap-1.5 text-xs text-foreground truncate">
            <User className="h-3 w-3 shrink-0 text-muted-foreground" />
            <span className="truncate">{name}</span>
          </div>
        );
      },
    },
    {
      id: "linked",
      header: t("table.linkedRecord"),
      cell: ({ row }) => {
        const task = row.original;
        return (
          <div className="flex flex-wrap items-center gap-1.5 text-xs">
            {task.dealId && (
              <Link
                href={dealRecordRoute(task.dealId)}
                className="inline-flex items-center gap-1 rounded-xs bg-muted px-1.5 py-0.5 text-[11px] font-medium text-foreground hover:bg-muted/80 truncate max-w-[140px]"
              >
                <Kanban className="h-3 w-3 shrink-0 text-muted-foreground" />
                <span className="truncate">{task.dealTitle ?? t("deal")}</span>
              </Link>
            )}
            {task.contactId && (
              <Link
                href={contactRecordRoute(task.contactId)}
                className="inline-flex items-center gap-1 rounded-xs bg-muted px-1.5 py-0.5 text-[11px] font-medium text-foreground hover:bg-muted/80 truncate max-w-[140px]"
              >
                <User className="h-3 w-3 shrink-0 text-muted-foreground" />
                <span className="truncate">
                  {task.contactName ?? t("contact")}
                </span>
              </Link>
            )}
            {task.companyId && (
              <Link
                href={companyRecordRoute(task.companyId)}
                className="inline-flex items-center gap-1 rounded-xs bg-muted px-1.5 py-0.5 text-[11px] font-medium text-foreground hover:bg-muted/80 truncate max-w-[140px]"
              >
                <Building2 className="h-3 w-3 shrink-0 text-muted-foreground" />
                <span className="truncate">
                  {task.companyName ?? t("company")}
                </span>
              </Link>
            )}
            {!task.dealId && !task.contactId && !task.companyId && (
              <span className="text-muted-foreground">—</span>
            )}
          </div>
        );
      },
    },
    {
      id: "actions",
      header: "",
      size: 70,
      cell: ({ row }) => {
        const task = row.original;
        return (
          <div className="flex items-center justify-end gap-1">
            <Can permission={PERMISSIONS.tasksWrite}>
              <button
                type="button"
                onClick={() => {
                  setEditingTask(task);
                  setTaskModalOpen(true);
                }}
                aria-label={t("editTask")}
                className="rounded-xs p-1 text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              >
                <Edit2 className="h-3.5 w-3.5" />
              </button>
            </Can>
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
        );
      },
    },
  ];

  return (
    <>
      <DataTable
        data={tasks}
        columns={columns}
        getRowId={(row) => row.id}
        loading={isLoading}
        isFetching={isFetching}
        manualPagination={Boolean(paginationSlot)}
        manualSorting={Boolean(sorting && onSortingChange)}
        sorting={sorting}
        onSortingChange={onSortingChange}
        paginationSlot={paginationSlot}
        hideToolbar
        emptyTitle={t("emptyTasks")}
      />

      <TaskModal
        open={taskModalOpen}
        onOpenChange={setTaskModalOpen}
        task={editingTask}
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
    </>
  );
}
