"use client";

import {
  keepPreviousData,
  queryOptions,
  useQuery,
} from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import type { Task, TaskFilters, TaskPage } from "@/features/tasks/types";
import { backendJson } from "@/lib/api/bff-client";

export function tasksListQueryOptions(
  filters: TaskFilters = {},
  locale: string,
) {
  return queryOptions({
    queryKey: queryKeys.tasksList(filters as Record<string, unknown>),
    queryFn: () =>
      backendJson<TaskPage>("/tasks", {
        locale,
        query: {
          after: filters.after ?? undefined,
          limit: filters.limit ?? LIMITS.tasksPageSize,
          q: filters.q?.trim() || undefined,
          assigneeUserId: filters.assigneeUserId || undefined,
          contactId: filters.contactId || undefined,
          dealId: filters.dealId || undefined,
          companyId: filters.companyId || undefined,
          status: filters.status || undefined,
          sort: filters.sort || undefined,
          order: filters.order || undefined,
        },
      }),
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

export function tasksMineQueryOptions(
  filters: TaskFilters = {},
  locale: string,
) {
  return queryOptions({
    queryKey: queryKeys.tasksMine(filters as Record<string, unknown>),
    queryFn: () =>
      backendJson<TaskPage>("/tasks/mine", {
        locale,
        query: {
          after: filters.after ?? undefined,
          limit: filters.limit ?? LIMITS.tasksPageSize,
          q: filters.q?.trim() || undefined,
          contactId: filters.contactId || undefined,
          dealId: filters.dealId || undefined,
          companyId: filters.companyId || undefined,
          status: filters.status || undefined,
          sort: filters.sort || undefined,
          order: filters.order || undefined,
        },
      }),
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

export function taskQueryOptions(id: string, locale: string) {
  return queryOptions({
    queryKey: queryKeys.task(id),
    queryFn: () =>
      backendJson<Task>(
        "/tasks/{id}",
        {
          locale,
        },
        `/tasks/${encodeURIComponent(id)}`,
      ),
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

export function useTasks(
  filters: TaskFilters = {},
  options?: { enabled?: boolean },
) {
  const locale = useLocale();
  return useQuery({
    ...tasksListQueryOptions(filters, locale),
    enabled: options?.enabled ?? true,
  });
}

export function useMyTasks(
  filters: TaskFilters = {},
  options?: { enabled?: boolean },
) {
  const locale = useLocale();
  return useQuery({
    ...tasksMineQueryOptions(filters, locale),
    enabled: options?.enabled ?? true,
  });
}

export function useTask(id: string | null | undefined) {
  const locale = useLocale();
  return useQuery({
    ...taskQueryOptions(id ?? "", locale),
    enabled: Boolean(id),
  });
}

export function overdueTasksQueryOptions(locale: string) {
  return queryOptions({
    queryKey: queryKeys.tasksOverdueCount(),
    queryFn: async () => {
      const page = await backendJson<TaskPage>("/tasks/mine", {
        locale,
        query: {
          status: "overdue",
          limit: 1,
        },
      });
      return page.items?.length ?? 0;
    },
    staleTime: LIMITS.queryStaleTimeMs,
  });
}

export function useOverdueTasksCount(options?: { enabled?: boolean }) {
  const locale = useLocale();
  return useQuery({
    ...overdueTasksQueryOptions(locale),
    enabled: options?.enabled ?? true,
  });
}

export function useSubjectTasks(
  subject: { contactId?: string; dealId?: string; companyId?: string },
  options?: { enabled?: boolean },
) {
  const locale = useLocale();
  const hasSubject = Boolean(
    subject.contactId || subject.dealId || subject.companyId,
  );
  return useQuery({
    ...tasksListQueryOptions(
      {
        contactId: subject.contactId,
        dealId: subject.dealId,
        companyId: subject.companyId,
        limit: LIMITS.maxTasksPageSize,
        sort: "due",
        order: "asc",
      },
      locale,
    ),
    enabled: (options?.enabled ?? true) && hasSubject,
  });
}
