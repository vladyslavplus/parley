import { renderHook } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { describe, expect, it, vi, beforeEach } from "vitest";

import { queryKeys } from "@/constants/query-keys";
import type { Task } from "@/features/tasks/types";
import { ApiError } from "@/lib/errors";
import { useTaskMutations } from "./use-task-mutations";

const mockToastError = vi.fn();

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/lib/toasts", () => ({
  showErrorToast: (...args: unknown[]) => mockToastError(...args),
  showSuccessToast: vi.fn(),
  showInfoToast: vi.fn(),
  showWarningToast: vi.fn(),
}));

const mockBackendJson = vi.fn();
const mockBackendVoid = vi.fn();

vi.mock("@/lib/api/bff-client", () => ({
  backendJson: (...args: unknown[]) => mockBackendJson(...args),
  backendVoid: (...args: unknown[]) => mockBackendVoid(...args),
}));

describe("useTaskMutations", () => {
  let queryClient: QueryClient;

  beforeEach(() => {
    vi.clearAllMocks();
    queryClient = new QueryClient({
      defaultOptions: {
        queries: { retry: false },
      },
    });
  });

  const wrapper = ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );

  const sampleTask: Task = {
    id: "task-1",
    title: "Follow up with client",
    dueAt: "2026-11-10T10:00:00Z",
    assigneeUserId: "user-1",
    assigneeDisplayName: "Alex Mercer",
    assigneeEmail: null,
    contactId: "contact-1",
    contactName: "John Doe",
    dealId: "deal-1",
    dealTitle: "Enterprise Expansion",
    companyId: "company-1",
    companyName: "Acme Corp",
    notes: "Review contract terms",
    isCompleted: false,
    completedAt: null,
    version: 1,
    createdAt: "2026-11-01T12:00:00Z",
    updatedAt: "2026-11-01T12:00:00Z",
  };

  it("completes task and invalidates tasks and activities caches", async () => {
    const completedTask: Task = {
      ...sampleTask,
      isCompleted: true,
      completedAt: "2026-11-09T12:00:00Z",
      version: 2,
    };
    mockBackendJson.mockResolvedValueOnce(completedTask);

    queryClient.setQueryData(queryKeys.task(sampleTask.id), sampleTask);
    const tasksAllKey = queryKeys.tasksAll();
    const activitiesAllKey = queryKeys.activitiesAll();
    queryClient.setQueryData(tasksAllKey, []);
    queryClient.setQueryData(activitiesAllKey, []);

    const { result } = renderHook(() => useTaskMutations(), { wrapper });

    const updated = await result.current.completeTask(
      sampleTask.id,
      sampleTask.version,
    );

    expect(mockBackendJson).toHaveBeenCalledWith(
      "/tasks/{id}/complete",
      {
        method: "POST",
        body: { version: 1 },
        locale: "en",
      },
      "/tasks/task-1/complete",
    );
    expect(updated.isCompleted).toBe(true);
    expect(queryClient.getQueryState(tasksAllKey)?.isInvalidated).toBe(true);
    expect(queryClient.getQueryState(activitiesAllKey)?.isInvalidated).toBe(
      true,
    );
    expect(queryClient.getQueryData(queryKeys.task(sampleTask.id))).toEqual(
      completedTask,
    );
  });

  it("handles optimistic complete with rollback on error", async () => {
    mockBackendJson.mockRejectedValueOnce(
      new ApiError({
        kind: "conflict",
        status: 409,
        code: "concurrency_conflict",
        detail: "Concurrent edit",
        traceId: null,
        fieldErrors: {},
      }),
    );

    queryClient.setQueryData(queryKeys.task(sampleTask.id), sampleTask);
    const { result } = renderHook(() => useTaskMutations(), { wrapper });

    await expect(
      result.current.completeTask(sampleTask.id, sampleTask.version),
    ).rejects.toThrow();

    // Verify rollback to previous state
    const cached = queryClient.getQueryData<Task>(
      queryKeys.task(sampleTask.id),
    );
    expect(cached?.isCompleted).toBe(false);
    expect(mockToastError).toHaveBeenCalled();
  });

  it("handles optimistic reopen with rollback on error", async () => {
    const completedTask: Task = {
      ...sampleTask,
      isCompleted: true,
      completedAt: "2026-11-09T12:00:00Z",
      version: 2,
    };
    mockBackendJson.mockRejectedValueOnce(
      new ApiError({
        kind: "server",
        status: 500,
        code: "server_error",
        detail: "Server failed",
        traceId: null,
        fieldErrors: {},
      }),
    );

    queryClient.setQueryData(queryKeys.task(sampleTask.id), completedTask);
    const { result } = renderHook(() => useTaskMutations(), { wrapper });

    await expect(
      result.current.reopenTask(sampleTask.id, completedTask.version),
    ).rejects.toThrow();

    // Verify rollback to previous state
    const cached = queryClient.getQueryData<Task>(
      queryKeys.task(sampleTask.id),
    );
    expect(cached?.isCompleted).toBe(true);
    expect(mockToastError).toHaveBeenCalled();
  });

  it("handles idempotent repeat complete gracefully (200 OK)", async () => {
    const alreadyCompletedTask: Task = {
      ...sampleTask,
      isCompleted: true,
      completedAt: "2026-11-09T12:00:00Z",
      version: 2,
    };
    mockBackendJson.mockResolvedValueOnce(alreadyCompletedTask);

    queryClient.setQueryData(
      queryKeys.task(sampleTask.id),
      alreadyCompletedTask,
    );
    const { result } = renderHook(() => useTaskMutations(), { wrapper });

    const res = await result.current.completeTask(sampleTask.id, 2);
    expect(res.isCompleted).toBe(true);
    expect(queryClient.getQueryData(queryKeys.task(sampleTask.id))).toEqual(
      alreadyCompletedTask,
    );
  });

  it("creates task and invalidates tasks and activities caches", async () => {
    mockBackendJson.mockResolvedValueOnce(sampleTask);

    const tasksAllKey = queryKeys.tasksAll();
    const activitiesAllKey = queryKeys.activitiesAll();
    queryClient.setQueryData(tasksAllKey, []);
    queryClient.setQueryData(activitiesAllKey, []);

    const { result } = renderHook(() => useTaskMutations(), { wrapper });

    const created = await result.current.createTask({
      title: "Follow up with client",
      assigneeUserId: "user-1",
      notes: null,
      dueAt: null,
      contactId: null,
      dealId: null,
      companyId: null,
    });

    expect(created).toEqual(sampleTask);
    expect(queryClient.getQueryState(tasksAllKey)?.isInvalidated).toBe(true);
    expect(queryClient.getQueryState(activitiesAllKey)?.isInvalidated).toBe(
      true,
    );
  });

  it("deletes task and invalidates caches", async () => {
    mockBackendVoid.mockResolvedValueOnce(undefined);

    const taskKey = queryKeys.task(sampleTask.id);
    const tasksAllKey = queryKeys.tasksAll();
    const activitiesAllKey = queryKeys.activitiesAll();
    queryClient.setQueryData(taskKey, sampleTask);
    queryClient.setQueryData(tasksAllKey, []);
    queryClient.setQueryData(activitiesAllKey, []);

    const { result } = renderHook(() => useTaskMutations(), { wrapper });

    await result.current.deleteTask(sampleTask.id);

    expect(mockBackendVoid).toHaveBeenCalledWith(
      "/tasks/{id}",
      {
        method: "DELETE",
        locale: "en",
      },
      "/tasks/task-1",
    );
    expect(queryClient.getQueryState(taskKey)?.isInvalidated).toBe(true);
    expect(queryClient.getQueryState(tasksAllKey)?.isInvalidated).toBe(true);
    expect(queryClient.getQueryState(activitiesAllKey)?.isInvalidated).toBe(
      true,
    );
  });
});
