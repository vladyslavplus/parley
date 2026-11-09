import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

import type { Task } from "@/features/tasks/types";
import { TaskModal } from "./task-modal";

const mockCreateTask = vi.fn();
const mockUpdateTask = vi.fn();

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({
    data: {
      user: { id: "u-me", fullName: "Current User" },
      organization: { timeZone: "UTC", currency: "USD" },
    },
  }),
}));

vi.mock("@/features/tasks/hooks/use-task-mutations", () => ({
  useTaskMutations: () => ({
    createTask: mockCreateTask,
    updateTask: mockUpdateTask,
  }),
}));

vi.mock("@/features/crm/components/owner-picker", () => ({
  OwnerPicker: ({
    value,
    onChange,
  }: {
    value: string;
    onChange: (id: string, item: unknown) => void;
  }) => (
    <div data-testid="owner-picker">
      <input
        data-testid="owner-picker-input"
        value={value ?? ""}
        onChange={(e) =>
          onChange(e.target.value, {
            id: e.target.value,
            fullName: "Selected Owner",
          })
        }
      />
    </div>
  ),
}));

vi.mock("@/features/deals/components/contact-picker", () => ({
  ContactPicker: () => <div data-testid="contact-picker" />,
}));

vi.mock("@/features/deals/components/deal-picker", () => ({
  DealPicker: () => <div data-testid="deal-picker" />,
}));

vi.mock("@/features/crm/components/company-picker", () => ({
  CompanyPicker: () => <div data-testid="company-picker" />,
}));

vi.mock("@/lib/toasts", () => ({
  showSuccessToast: vi.fn(),
  showErrorToast: vi.fn(),
}));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

describe("TaskModal", () => {
  it("shows validation error when title is empty on create", async () => {
    render(<TaskModal open={true} onOpenChange={vi.fn()} />);

    const submitBtn = screen.getByRole("button", { name: "create" });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(screen.getByText("errors.titleRequired")).toBeDefined();
    });
    expect(mockCreateTask).not.toHaveBeenCalled();
  });

  it("submits valid task creation", async () => {
    mockCreateTask.mockResolvedValueOnce({
      id: "task-1",
      title: "New Follow Up",
      dueAt: null,
      assigneeUserId: "u-me",
      isCompleted: false,
      completedAt: null,
      version: 1,
      createdAt: "2026-11-09T00:00:00Z",
      updatedAt: "2026-11-09T00:00:00Z",
    });

    const onSaved = vi.fn();
    const onOpenChange = vi.fn();

    render(
      <TaskModal open={true} onOpenChange={onOpenChange} onSaved={onSaved} />,
    );

    const titleInput = screen.getByPlaceholderText("form.titlePlaceholder");
    fireEvent.change(titleInput, { target: { value: "New Follow Up" } });

    const submitBtn = screen.getByRole("button", { name: "create" });
    fireEvent.click(submitBtn);

    await waitFor(() => {
      expect(mockCreateTask).toHaveBeenCalledWith(
        expect.objectContaining({
          title: "New Follow Up",
          assigneeUserId: "u-me",
        }),
      );
    });

    expect(onSaved).toHaveBeenCalled();
    expect(onOpenChange).toHaveBeenCalledWith(false);
  });

  it("disables save button while form is clean in edit mode", async () => {
    const existingTask: Task = {
      id: "t-1",
      title: "Existing Task",
      dueAt: null,
      assigneeUserId: "u-me",
      assigneeDisplayName: "Current User",
      assigneeEmail: null,
      contactId: null,
      contactName: null,
      dealId: null,
      dealTitle: null,
      companyId: null,
      companyName: null,
      notes: "Some notes",
      isCompleted: false,
      completedAt: null,
      version: 3,
      createdAt: "2026-11-01T00:00:00Z",
      updatedAt: "2026-11-01T00:00:00Z",
    };

    render(
      <TaskModal open={true} onOpenChange={vi.fn()} task={existingTask} />,
    );

    const saveBtn = screen.getByRole("button", { name: "save" });
    expect(saveBtn).toBeDisabled();

    // Changing title marks form dirty and enables save button
    const titleInput = screen.getByDisplayValue("Existing Task");
    fireEvent.change(titleInput, { target: { value: "Updated Task Title" } });

    await waitFor(() => {
      expect(saveBtn).not.toBeDisabled();
    });
  });

  it("updates task on edit form submission", async () => {
    const existingTask: Task = {
      id: "t-1",
      title: "Existing Task",
      dueAt: null,
      assigneeUserId: "u-me",
      assigneeDisplayName: "Current User",
      assigneeEmail: null,
      contactId: null,
      contactName: null,
      dealId: null,
      dealTitle: null,
      companyId: null,
      companyName: null,
      notes: "Some notes",
      isCompleted: false,
      completedAt: null,
      version: 3,
      createdAt: "2026-11-01T00:00:00Z",
      updatedAt: "2026-11-01T00:00:00Z",
    };

    mockUpdateTask.mockResolvedValueOnce({
      ...existingTask,
      title: "Updated Task Title",
      version: 4,
    });

    render(
      <TaskModal open={true} onOpenChange={vi.fn()} task={existingTask} />,
    );

    const titleInput = screen.getByDisplayValue("Existing Task");
    fireEvent.change(titleInput, { target: { value: "Updated Task Title" } });

    const saveBtn = screen.getByRole("button", { name: "save" });
    fireEvent.click(saveBtn);

    await waitFor(() => {
      expect(mockUpdateTask).toHaveBeenCalledWith(
        "t-1",
        expect.objectContaining({
          version: 3,
          title: "Updated Task Title",
        }),
      );
    });
  });
});
