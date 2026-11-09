"use client";

import { Plus } from "lucide-react";
import { useTranslations } from "next-intl";
import { parseAsString, parseAsStringLiteral, useQueryState } from "nuqs";
import { useState } from "react";

import { DataTableSearchFilterBar } from "@/components/data-table/data-table-search-filter-bar";
import { TableProgressBar } from "@/components/data-table/table-progress-bar";
import { useServerTableState } from "@/components/data-table/use-server-table-state";
import { Button } from "@/components/ui/button";
import {
  SegmentedControl,
  type SegmentedOption,
} from "@/components/ui/segmented-control";
import { PERMISSIONS } from "@/constants/permissions";
import { KeysetPagination } from "@/features/crm/components/keyset-pagination";
import { useOwnerDimension } from "@/features/crm/filters/use-filter-dimensions";
import { TaskModal } from "@/features/tasks/components/task-modal";
import { TasksTable } from "@/features/tasks/components/tasks-table";
import { useMyTasks, useTasks } from "@/features/tasks/hooks/use-tasks";
import { Can, useCan } from "@/features/session/components/can";
import { cn } from "@/lib/utils";

const TASK_STATUSES = ["all", "open", "completed", "overdue"] as const;
type TaskStatusFilter = (typeof TASK_STATUSES)[number];

const TASK_TABS = ["mine", "all"] as const;
type TaskTab = (typeof TASK_TABS)[number];

const TASK_SORTS = ["due", "title", "created"] as const;

export function TasksListView() {
  const t = useTranslations("crm.tasks");
  const canReadAll = useCan(PERMISSIONS.tasksRead);

  const [tab, setTab] = useQueryState(
    "tab",
    parseAsStringLiteral(TASK_TABS).withDefault("mine"),
  );
  const [status, setStatus] = useQueryState(
    "status",
    parseAsStringLiteral(TASK_STATUSES).withDefault("open"),
  );
  const [assigneeUserId, setAssigneeUserId] = useQueryState(
    "assigneeUserId",
    parseAsString.withDefault(""),
  );

  const tableState = useServerTableState({
    defaultSort: "due",
    defaultOrder: "asc",
    allowedSorts: TASK_SORTS,
  });

  const [createModalOpen, setCreateModalOpen] = useState(false);

  const isMine = tab === "mine";

  const assigneeDimension = useOwnerDimension({
    value: assigneeUserId,
    label: t("table.assignee"),
    onChange: (value) => {
      void setAssigneeUserId(value);
      tableState.resetPaging();
    },
  });

  const queryFilters = {
    after: tableState.after,
    limit: tableState.pageSize,
    q: tableState.q || undefined,
    status: status === "all" ? undefined : status,
    assigneeUserId: !isMine ? assigneeUserId || undefined : undefined,
    sort: (tableState.sort as "due" | "title" | "created") || "due",
    order: tableState.order,
  };

  const myTasksQuery = useMyTasks(queryFilters, { enabled: isMine });
  const allTasksQuery = useTasks(queryFilters, {
    enabled: !isMine && canReadAll,
  });

  const activeQuery = isMine ? myTasksQuery : allTasksQuery;
  const tasks = activeQuery.data?.items ?? [];

  const hasActiveFilters = Boolean(
    tableState.q ||
    tableState.after ||
    status !== "open" ||
    (!isMine && assigneeUserId),
  );

  function handleResetFilters() {
    tableState.resetAll(() => {
      void setStatus("open");
      void setAssigneeUserId("");
    });
  }

  function handleTabChange(nextTab: TaskTab) {
    if (nextTab === tab) return;
    void setTab(nextTab);
    tableState.resetPaging();
  }

  const statusOptions: SegmentedOption<TaskStatusFilter>[] = [
    { value: "open", label: t("status.open") },
    { value: "overdue", label: t("status.overdue") },
    { value: "completed", label: t("status.completed") },
    { value: "all", label: t("status.all") },
  ];

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="text-xl font-bold tracking-tight text-foreground">
            {t("title")}
          </h1>
          <p className="text-xs text-muted-foreground mt-0.5">
            {t("subtitle")}
          </p>
        </div>

        <Can permission={PERMISSIONS.tasksWrite}>
          <Button
            type="button"
            onClick={() => setCreateModalOpen(true)}
            className="text-xs"
          >
            <Plus className="h-4 w-4" />
            <span>{t("newTask")}</span>
          </Button>
        </Can>
      </div>

      {/* Tabs & Toolbar */}
      <div className="space-y-3">
        {canReadAll && (
          <div className="flex border-b border-border">
            <button
              type="button"
              onClick={() => handleTabChange("mine")}
              className={cn(
                "pb-2.5 px-3 text-xs font-semibold transition-colors border-b-2 -mb-px",
                tab === "mine"
                  ? "border-primary text-foreground"
                  : "border-transparent text-muted-foreground hover:text-foreground",
              )}
            >
              {t("tabs.mine")}
            </button>
            <button
              type="button"
              onClick={() => handleTabChange("all")}
              className={cn(
                "pb-2.5 px-3 text-xs font-semibold transition-colors border-b-2 -mb-px",
                tab === "all"
                  ? "border-primary text-foreground"
                  : "border-transparent text-muted-foreground hover:text-foreground",
              )}
            >
              {t("tabs.all")}
            </button>
          </div>
        )}

        <DataTableSearchFilterBar
          searchInput={tableState.searchInput}
          onSearchInputChange={tableState.setSearchInput}
          onClearSearch={tableState.clearSearch}
          searchPlaceholder={t("searchPlaceholder")}
          filters={isMine ? undefined : [assigneeDimension]}
          hasActiveFilters={hasActiveFilters}
          onResetFilters={handleResetFilters}
        >
          <SegmentedControl
            value={status}
            options={statusOptions}
            ariaLabel={t("statusLabel")}
            onChange={(next) => {
              void setStatus(next);
              tableState.resetPaging();
            }}
          />
        </DataTableSearchFilterBar>
      </div>

      <TableProgressBar active={activeQuery.isFetching} />

      {/* Table */}
      <TasksTable
        tasks={tasks}
        isLoading={activeQuery.isPending && !activeQuery.data}
        isFetching={activeQuery.isFetching}
        sorting={tableState.sorting}
        onSortingChange={tableState.onSortingChange}
        paginationSlot={
          <KeysetPagination
            hasPreviousPage={tableState.hasPreviousPage}
            hasNextPage={Boolean(activeQuery.data?.nextCursor)}
            onPreviousPage={tableState.handlePreviousPage}
            onNextPage={() =>
              tableState.handleNextPage(activeQuery.data?.nextCursor ?? null)
            }
            pageSize={tableState.pageSize}
            onPageSizeChange={tableState.setPageSize}
            itemCount={tasks.length}
          />
        }
      />

      <TaskModal open={createModalOpen} onOpenChange={setCreateModalOpen} />
    </div>
  );
}
