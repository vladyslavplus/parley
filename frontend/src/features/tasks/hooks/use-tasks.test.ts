import { beforeEach, describe, expect, it, vi } from "vitest";

const backendJson = vi.hoisted(() => vi.fn());
vi.mock("@/lib/api/bff-client", () => ({ backendJson }));

const { overdueTasksQueryOptions, tasksListQueryOptions } =
  await import("@/features/tasks/hooks/use-tasks");

beforeEach(() => {
  backendJson.mockReset();
  backendJson.mockResolvedValue({ count: 3, items: [] });
});

describe("task type filter", () => {
  it("goes to the list with the other filters", async () => {
    await tasksListQueryOptions(
      { type: "call", status: "overdue" },
      "en",
    ).queryFn?.({} as never);

    expect(backendJson).toHaveBeenCalledWith(
      "/tasks",
      expect.objectContaining({
        query: expect.objectContaining({ type: "call", status: "overdue" }),
      }),
    );
  });

  it("goes to both overdue counts, so a count follows the list", async () => {
    await overdueTasksQueryOptions("all", { type: "meeting" }, "en").queryFn?.(
      {} as never,
    );
    await overdueTasksQueryOptions("mine", { type: "meeting" }, "en").queryFn?.(
      {} as never,
    );

    expect(backendJson).toHaveBeenNthCalledWith(
      1,
      "/tasks/overdue-count",
      expect.objectContaining({
        query: expect.objectContaining({ type: "meeting" }),
      }),
    );
    expect(backendJson).toHaveBeenNthCalledWith(
      2,
      "/tasks/mine/overdue-count",
      expect.objectContaining({
        query: expect.objectContaining({ type: "meeting" }),
      }),
    );
  });

  it("keeps counts of different types apart in the cache", () => {
    const call = overdueTasksQueryOptions("all", { type: "call" }, "en");
    const email = overdueTasksQueryOptions("all", { type: "email" }, "en");
    expect(call.queryKey).not.toEqual(email.queryKey);
  });
});
