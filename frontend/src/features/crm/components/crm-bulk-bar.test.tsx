import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";

import { PERMISSIONS } from "@/constants/permissions";
import { ApiError } from "@/lib/errors";
import { CrmBulkBar } from "./crm-bulk-bar";

beforeAll(() => {
  window.HTMLElement.prototype.scrollIntoView = vi.fn();
  window.HTMLElement.prototype.hasPointerCapture = vi.fn();
  window.HTMLElement.prototype.releasePointerCapture = vi.fn();
});

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string) => key,
}));

let activePermissions: string[] = [];
vi.mock("@/features/session/components/can", () => ({
  useCan: (perm: string) => activePermissions.includes(perm),
  Can: ({
    permission,
    children,
  }: {
    permission: string;
    children: React.ReactNode;
  }) => (activePermissions.includes(permission) ? children : null),
}));

vi.mock("@/lib/api/hooks/use-api", () => ({
  useMembers: () => ({
    data: {
      pages: [
        {
          items: [
            {
              userId: "u-1",
              displayName: "Alice",
              email: "alice@test.local",
              status: "active",
              roleCode: "sales_rep",
            },
          ],
        },
      ],
    },
  }),
}));

afterEach(cleanup);

describe("CrmBulkBar permissions and actions", () => {
  it("hides delete button when user lacks delete permission (sales_rep role)", () => {
    activePermissions = [PERMISSIONS.contactsWrite];

    render(
      <CrmBulkBar
        selectedCount={3}
        writePermission={PERMISSIONS.contactsWrite}
        deletePermission={PERMISSIONS.contactsDelete}
        onAssignOwner={vi.fn()}
        onDelete={vi.fn()}
        clearSelection={vi.fn()}
      />,
    );

    expect(screen.getByText("assignOwner")).toBeInTheDocument();
    expect(screen.queryByText("delete")).toBeNull();
  });

  it("shows delete button when user has delete permission (manager or admin)", async () => {
    activePermissions = [PERMISSIONS.contactsWrite, PERMISSIONS.contactsDelete];

    const onDelete = vi.fn();
    render(
      <CrmBulkBar
        selectedCount={2}
        writePermission={PERMISSIONS.contactsWrite}
        deletePermission={PERMISSIONS.contactsDelete}
        onAssignOwner={vi.fn()}
        onDelete={onDelete}
        clearSelection={vi.fn()}
      />,
    );

    expect(screen.getByText("assignOwner")).toBeInTheDocument();
    const deleteBtn = screen.getByText("delete");
    expect(deleteBtn).toBeInTheDocument();
  });

  it("opens assign owner dialog and triggers onAssignOwner", async () => {
    activePermissions = [PERMISSIONS.contactsWrite];
    const onAssign = vi.fn().mockResolvedValue(undefined);
    const clearSelection = vi.fn();

    render(
      <CrmBulkBar
        selectedCount={2}
        writePermission={PERMISSIONS.contactsWrite}
        deletePermission={PERMISSIONS.contactsDelete}
        onAssignOwner={onAssign}
        onDelete={vi.fn()}
        clearSelection={clearSelection}
      />,
    );

    fireEvent.click(screen.getByText("assignOwner"));
    expect(
      screen.getByRole("heading", { name: "assignOwnerDialogTitle" }),
    ).toBeInTheDocument();

    const trigger = screen.getByRole("combobox");
    fireEvent.click(trigger);

    const option = await screen.findByText(/Alice/);
    fireEvent.click(option);

    fireEvent.click(screen.getByRole("button", { name: "assign" }));

    await waitFor(() => {
      expect(onAssign).toHaveBeenCalledWith("u-1");
      expect(clearSelection).toHaveBeenCalled();
    });
  });

  it("shows error and keeps selection when onAssignOwner fails", async () => {
    activePermissions = [PERMISSIONS.contactsWrite];
    const error = new ApiError({
      kind: "not_found",
      status: 404,
      code: "crm.contact.not_found",
      detail: "Row not found",
      traceId: null,
      fieldErrors: {},
    });
    const onAssign = vi.fn().mockRejectedValue(error);
    const clearSelection = vi.fn();

    render(
      <CrmBulkBar
        selectedCount={2}
        writePermission={PERMISSIONS.contactsWrite}
        deletePermission={PERMISSIONS.contactsDelete}
        onAssignOwner={onAssign}
        onDelete={vi.fn()}
        clearSelection={clearSelection}
      />,
    );

    fireEvent.click(screen.getByText("assignOwner"));

    const trigger = screen.getByRole("combobox");
    fireEvent.click(trigger);
    const option = await screen.findByText(/Alice/);
    fireEvent.click(option);

    fireEvent.click(screen.getByRole("button", { name: "assign" }));

    await waitFor(() => {
      expect(screen.getByRole("alert")).toBeInTheDocument();
      expect(screen.getByText("Row not found")).toBeInTheDocument();
    });

    expect(clearSelection).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "assign" })).not.toBeDisabled();
  });

  it("closes dialog on Escape key", async () => {
    activePermissions = [PERMISSIONS.contactsWrite];

    render(
      <CrmBulkBar
        selectedCount={2}
        writePermission={PERMISSIONS.contactsWrite}
        deletePermission={PERMISSIONS.contactsDelete}
        onAssignOwner={vi.fn()}
        onDelete={vi.fn()}
        clearSelection={vi.fn()}
      />,
    );

    fireEvent.click(screen.getByText("assignOwner"));
    expect(
      screen.getByRole("heading", { name: "assignOwnerDialogTitle" }),
    ).toBeInTheDocument();

    fireEvent.keyDown(document.body, {
      key: "Escape",
      code: "Escape",
    });

    await waitFor(() => {
      expect(
        screen.queryByRole("heading", { name: "assignOwnerDialogTitle" }),
      ).toBeNull();
    });
  });
});
