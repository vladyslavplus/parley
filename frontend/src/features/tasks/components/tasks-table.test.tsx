import { cleanup, render, screen, within } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it, vi } from "vitest";

import crmEn from "../../../../messages/en/crm.json";
import tableEn from "../../../../messages/en/table.json";
import type { Task } from "@/features/tasks/types";

vi.mock("@/features/tasks/hooks/use-task-mutations", () => ({
  useTaskMutations: () => ({ deleteTask: vi.fn() }),
}));
vi.mock("@/features/tasks/components/task-status-checkbox", () => ({
  TaskStatusCheckbox: () => <span role="checkbox" aria-checked="false" />,
}));
vi.mock("@/features/tasks/components/task-modal", () => ({
  TaskModal: () => null,
}));
vi.mock("@/features/session/components/can", () => ({
  Can: ({ children }: { children: React.ReactNode }) => children,
}));
vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({ data: { user: { id: "u-1", timeZone: "UTC" } } }),
}));
vi.mock("@/i18n/navigation", () => ({
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
}));

const { TasksTable } = await import("@/features/tasks/components/tasks-table");

const task = {
  id: "t-1",
  title: "Confirm order",
  type: "call",
  notes: null,
  dueAt: "2020-01-01T10:00:00Z",
  completedAt: null,
  isCompleted: false,
  assigneeUserId: "u-1",
  assigneeDisplayName: "Maria Lopez",
  assigneeEmail: null,
  contactId: null,
  contactName: null,
  dealId: null,
  dealTitle: null,
  companyId: null,
  companyName: null,
  createdAt: "2020-01-01T00:00:00Z",
  updatedAt: "2020-01-01T00:00:00Z",
  version: 1,
} as Task;

afterEach(cleanup);

describe("tasks table sorting", () => {
  it("offers a sorter only on the columns the API can sort by", () => {
    render(
      <NextIntlClientProvider
        locale="en"
        messages={{ crm: crmEn, table: tableEn, common: {} }}
      >
        <TasksTable tasks={[task]} isLoading={false} />
      </NextIntlClientProvider>,
    );

    const sortable = screen
      .getAllByRole("columnheader")
      .filter((header) => within(header).queryByRole("button"))
      .map((header) => header.textContent?.trim());

    expect(sortable).toEqual(["Title", "Due Date"]);
  });
});
