"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";

import { queryKeys } from "@/constants/query-keys";
import type { Task, TaskPage } from "@/features/tasks/types";
import { backendJson, backendVoid } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";
import { describeError, toApiError } from "@/lib/errors";
import { showErrorToast } from "@/lib/toasts";

export function useTaskMutations() {
  const queryClient = useQueryClient();
  const locale = useLocale();
  const tErrors = useTranslations("errors");

  async function createTask(body: Schemas["TaskRequest"]): Promise<Task> {
    try {
      const created = await backendJson<Task>("/tasks", {
        method: "POST",
        body,
        locale,
      });

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.tasksAll() }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.tasksOverdueCount(),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
      ]);

      return created;
    } catch (err) {
      const apiErr = toApiError(err);
      showErrorToast(
        describeError(apiErr, (kind) =>
          tErrors(kind as Parameters<typeof tErrors>[0]),
        ),
      );
      throw apiErr;
    }
  }

  async function updateTask(
    id: string,
    body: Schemas["UpdateTaskRequest"],
  ): Promise<Task> {
    try {
      const updated = await backendJson<Task>(
        "/tasks/{id}",
        {
          method: "PUT",
          body,
          locale,
        },
        `/tasks/${encodeURIComponent(id)}`,
      );

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.task(id) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.tasksAll() }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.tasksOverdueCount(),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
      ]);

      return updated;
    } catch (err) {
      const apiErr = toApiError(err);
      showErrorToast(
        describeError(apiErr, (kind) =>
          tErrors(kind as Parameters<typeof tErrors>[0]),
        ),
      );
      throw apiErr;
    }
  }

  async function completeTask(
    id: string,
    version: number | string,
  ): Promise<Task> {
    const previousTask = queryClient.getQueryData<Task>(queryKeys.task(id));
    const previousLists = queryClient.getQueriesData<TaskPage>({
      queryKey: queryKeys.tasksAll(),
    });
    const previousOverdue = queryClient.getQueriesData<number>({
      queryKey: queryKeys.tasksOverdueCount(),
    });

    // Optimistic update of single task
    queryClient.setQueryData<Task | undefined>(queryKeys.task(id), (old) =>
      old
        ? {
            ...old,
            isCompleted: true,
            completedAt: new Date().toISOString(),
          }
        : undefined,
    );

    // Optimistic update of all task list caches
    queryClient.setQueriesData<TaskPage>(
      { queryKey: queryKeys.tasksAll() },
      (old) => {
        if (!old?.items) return old;
        return {
          ...old,
          items: old.items.map((item) =>
            item.id === id
              ? {
                  ...item,
                  isCompleted: true,
                  completedAt: new Date().toISOString(),
                }
              : item,
          ),
        };
      },
    );

    // Optimistic decrement of overdue count
    queryClient.setQueriesData<number>(
      { queryKey: queryKeys.tasksOverdueCount() },
      (count) => (typeof count === "number" ? Math.max(0, count - 1) : count),
    );

    try {
      const completed = await backendJson<Task>(
        "/tasks/{id}/complete",
        {
          method: "POST",
          body: { version },
          locale,
        },
        `/tasks/${encodeURIComponent(id)}/complete`,
      );

      queryClient.setQueryData(queryKeys.task(id), completed);

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.tasksAll() }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.tasksOverdueCount(),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
      ]);

      return completed;
    } catch (err) {
      // Rollback on error
      if (previousTask) {
        queryClient.setQueryData(queryKeys.task(id), previousTask);
      }
      for (const [key, data] of previousLists) {
        queryClient.setQueryData(key, data);
      }
      for (const [key, count] of previousOverdue) {
        queryClient.setQueryData(key, count);
      }
      await queryClient.invalidateQueries({ queryKey: queryKeys.tasksAll() });

      const apiErr = toApiError(err);
      showErrorToast(
        describeError(apiErr, (kind) =>
          tErrors(kind as Parameters<typeof tErrors>[0]),
        ),
      );
      throw apiErr;
    }
  }

  async function reopenTask(
    id: string,
    version: number | string,
  ): Promise<Task> {
    const previousTask = queryClient.getQueryData<Task>(queryKeys.task(id));
    const previousLists = queryClient.getQueriesData<TaskPage>({
      queryKey: queryKeys.tasksAll(),
    });
    const previousOverdue = queryClient.getQueriesData<number>({
      queryKey: queryKeys.tasksOverdueCount(),
    });

    // Optimistic update of single task
    queryClient.setQueryData<Task | undefined>(queryKeys.task(id), (old) =>
      old
        ? {
            ...old,
            isCompleted: false,
            completedAt: null,
          }
        : undefined,
    );

    // Optimistic update of all task list caches
    queryClient.setQueriesData<TaskPage>(
      { queryKey: queryKeys.tasksAll() },
      (old) => {
        if (!old?.items) return old;
        return {
          ...old,
          items: old.items.map((item) =>
            item.id === id
              ? {
                  ...item,
                  isCompleted: false,
                  completedAt: null,
                }
              : item,
          ),
        };
      },
    );

    try {
      const reopened = await backendJson<Task>(
        "/tasks/{id}/reopen",
        {
          method: "POST",
          body: { version },
          locale,
        },
        `/tasks/${encodeURIComponent(id)}/reopen`,
      );

      queryClient.setQueryData(queryKeys.task(id), reopened);

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.tasksAll() }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.tasksOverdueCount(),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
      ]);

      return reopened;
    } catch (err) {
      if (previousTask) {
        queryClient.setQueryData(queryKeys.task(id), previousTask);
      }
      for (const [key, data] of previousLists) {
        queryClient.setQueryData(key, data);
      }
      for (const [key, count] of previousOverdue) {
        queryClient.setQueryData(key, count);
      }
      await queryClient.invalidateQueries({ queryKey: queryKeys.tasksAll() });

      const apiErr = toApiError(err);
      showErrorToast(
        describeError(apiErr, (kind) =>
          tErrors(kind as Parameters<typeof tErrors>[0]),
        ),
      );
      throw apiErr;
    }
  }

  async function deleteTask(id: string): Promise<void> {
    try {
      await backendVoid(
        "/tasks/{id}",
        {
          method: "DELETE",
          locale,
        },
        `/tasks/${encodeURIComponent(id)}`,
      );

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.task(id) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.tasksAll() }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.tasksOverdueCount(),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.activitiesAll() }),
      ]);
    } catch (err) {
      const apiErr = toApiError(err);
      showErrorToast(
        describeError(apiErr, (kind) =>
          tErrors(kind as Parameters<typeof tErrors>[0]),
        ),
      );
      throw apiErr;
    }
  }

  return {
    createTask,
    updateTask,
    completeTask,
    reopenTask,
    deleteTask,
  };
}
