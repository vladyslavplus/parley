"use client";

import {
  keepPreviousData,
  queryOptions,
  useQuery,
} from "@tanstack/react-query";
import { useLocale } from "next-intl";

import { LIMITS } from "@/constants/limits";
import { queryKeys } from "@/constants/query-keys";
import { toNumber } from "@/features/campaigns/types";
import type {
  OverdueCountFilters,
  Task,
  TaskFilters,
  TaskOverdueCount,
  TaskPage,
} from "@/features/tasks/types";
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
          type: filters.type || undefined,
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
          type: filters.type || undefined,
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

export type OverdueScope = "mine" | "all";

// Empty values are left out, so the same filters always give the same key.
function normalizeOverdueFilters(filters: OverdueCountFilters) {
  return {
    q: filters.q?.trim() || undefined,
    assigneeUserId: filters.assigneeUserId || undefined,
    contactId: filters.contactId || undefined,
    dealId: filters.dealId || undefined,
    companyId: filters.companyId || undefined,
    type: filters.type || undefined,
  };
}

// "mine" counts the caller's own overdue tasks; "all" counts every task the
// caller may see. The endpoints take the filters of the list, nothing more.
export function overdueTasksQueryOptions(
  scope: OverdueScope,
  filters: OverdueCountFilters,
  locale: string,
) {
  const query = normalizeOverdueFilters(
    scope === "mine" ? { ...filters, assigneeUserId: undefined } : filters,
  );
  return queryOptions({
    queryKey: queryKeys.tasksOverdueCount(scope, query),
    queryFn: async () => {
      const view = await backendJson<TaskOverdueCount>(
        scope === "mine" ? "/tasks/mine/overdue-count" : "/tasks/overdue-count",
        { locale, query },
      );
      return toNumber(view.count);
    },
    staleTime: LIMITS.queryStaleTimeMs,
    placeholderData: keepPreviousData,
  });
}

export function useOverdueTasksCount(options?: {
  scope?: OverdueScope;
  filters?: OverdueCountFilters;
  enabled?: boolean;
}) {
  const locale = useLocale();
  return useQuery({
    ...overdueTasksQueryOptions(
      options?.scope ?? "mine",
      options?.filters ?? {},
      locale,
    ),
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

// The open tasks of a contact that are due now or late, soonest first: what the
// "Follow up" step of the priority list points at. Read when the popover opens.
export function useDueContactTasks(contactId: string, enabled: boolean) {
  const locale = useLocale();
  return useQuery({
    ...tasksListQueryOptions(
      {
        contactId,
        status: "overdue",
        limit: LIMITS.nextStepTasksLimit,
        sort: "due",
        order: "asc",
      },
      locale,
    ),
    enabled,
  });
}
