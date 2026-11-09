import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import type { Task } from "@/features/tasks/types";
import { TaskStatusCheckbox } from "./task-status-checkbox";

const mockCompleteTask = vi.fn();
const mockReopenTask = vi.fn();

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string, values?: Record<string, string>) =>
    `${key}:${values?.title ?? ""}`,
}));

vi.mock("@/features/tasks/hooks/use-task-mutations", () => ({
  useTaskMutations: () => ({
    completeTask: mockCompleteTask,
    reopenTask: mockReopenTask,
  }),
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("TaskStatusCheckbox", () => {
  const openTask: Task = {
    id: "task-1",
    title: "Client meeting",
    dueAt: null,
    assigneeUserId: "u-1",
    assigneeDisplayName: "Alex",
    assigneeEmail: null,
    contactId: null,
    contactName: null,
    dealId: null,
    dealTitle: null,
    companyId: null,
    companyName: null,
    notes: null,
    isCompleted: false,
    completedAt: null,
    version: 1,
    createdAt: "2026-11-09T00:00:00Z",
    updatedAt: "2026-11-09T00:00:00Z",
  };

  it("renders accessible checkbox and calls completeTask on click", async () => {
    mockCompleteTask.mockResolvedValueOnce({
      ...openTask,
      isCompleted: true,
      version: 2,
    });

    const onStatusChange = vi.fn();

    render(
      <TaskStatusCheckbox task={openTask} onStatusChange={onStatusChange} />,
    );

    const checkbox = screen.getByRole("checkbox", {
      name: "completeTaskAria:Client meeting",
    });
    expect(checkbox).toHaveAttribute("aria-checked", "false");

    fireEvent.click(checkbox);

    await waitFor(() => {
      expect(mockCompleteTask).toHaveBeenCalledWith("task-1", 1);
    });
    expect(onStatusChange).toHaveBeenCalled();
  });

  it("calls reopenTask when checked task is clicked", async () => {
    const completedTask: Task = {
      ...openTask,
      isCompleted: true,
      completedAt: "2026-11-09T10:00:00Z",
      version: 2,
    };

    mockReopenTask.mockResolvedValueOnce({
      ...completedTask,
      isCompleted: false,
      completedAt: null,
      version: 3,
    });

    render(<TaskStatusCheckbox task={completedTask} />);

    const checkbox = screen.getByRole("checkbox", {
      name: "reopenTaskAria:Client meeting",
    });
    expect(checkbox).toHaveAttribute("aria-checked", "true");

    fireEvent.click(checkbox);

    await waitFor(() => {
      expect(mockReopenTask).toHaveBeenCalledWith("task-1", 2);
    });
  });
});
