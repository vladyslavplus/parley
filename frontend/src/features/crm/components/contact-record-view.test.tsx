import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
  within,
} from "@testing-library/react";
import { NuqsTestingAdapter } from "nuqs/adapters/testing";
import { afterEach, describe, expect, it, vi } from "vitest";

const mockUpdateContact = vi.fn();
const mockDeleteContact = vi.fn();
const mockRestoreContact = vi.fn();
const mockUndoToast = vi.fn();
const mockRouterPush = vi.fn();

let mockCanWrite = true;
let mockCanReadOrders = true;

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string, params?: Record<string, unknown>) => {
    if (params?.date) return `${key} ${params.date}`;
    return key;
  },
}));

vi.mock("@/i18n/navigation", () => ({
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
  useRouter: () => ({ push: mockRouterPush }),
}));

vi.mock("@/lib/toasts", () => ({
  useUndoToast: () => mockUndoToast,
}));

vi.mock("@/features/session/components/can", () => ({
  Can: ({ children }: { children: React.ReactNode }) => children,
  useCan: (permission: string) =>
    permission === "orders.read" ? mockCanReadOrders : mockCanWrite,
}));

vi.mock("@/features/crm/hooks/use-contact", () => ({
  useContact: () => ({
    isPending: false,
    isError: false,
    data: {
      id: "c-100",
      firstName: "John",
      lastName: "Doe",
      email: "john@example.com",
      phone: "+380501112233",
      jobTitle: "CTO",
      companyId: null,
      ownerUserId: "u-1",
      tagIds: ["tag-1"],
      customFields: {
        active_key: "value1",
        archived_key: "retired_value",
      },
      createdAt: "2026-01-01T12:00:00Z",
      updatedAt: "2026-01-01T12:00:00Z",
      version: 42,
    },
  }),
}));

vi.mock("@/features/crm/hooks/use-custom-fields", () => ({
  useCustomFields: () => ({
    data: [
      {
        id: "cf-1",
        key: "active_key",
        label: "Active Field",
        fieldType: "text",
        archivedAt: null,
        position: 0,
      },
      {
        id: "cf-2",
        key: "archived_key",
        label: "Archived Field",
        fieldType: "text",
        archivedAt: "2026-01-02T00:00:00Z",
        position: 1,
      },
    ],
  }),
}));

vi.mock("@/features/crm/hooks/use-tags", () => ({
  useTags: () => ({
    data: {
      items: [
        {
          id: "tag-1",
          name: "VIP",
          colorIndex: 0,
        },
      ],
    },
  }),
}));

vi.mock("@/lib/api/hooks/use-api", () => ({
  useMembers: () => ({ data: { pages: [{ items: [] }] } }),
}));

vi.mock("@/features/crm/hooks/use-companies", () => ({
  useCompanies: () => ({ data: { items: [] } }),
}));

vi.mock("@/features/crm/hooks/use-owners", () => ({
  useOwners: () => ({ data: { items: [] }, isFetching: false }),
}));

vi.mock("@/features/crm/hooks/use-crm-mutations", () => ({
  useCrmMutations: () => ({
    updateContact: mockUpdateContact,
    deleteContact: mockDeleteContact,
    restoreContact: mockRestoreContact,
    attachContactTags: vi.fn(),
    detachContactTag: vi.fn(),
    createTag: vi.fn(),
  }),
}));

vi.mock("@/features/orders/components/orders-panel", () => ({
  OrdersPanel: () => <div>orders panel</div>,
}));

const { ContactRecordView } = await import("./contact-record-view");

function renderRecord(
  searchParams?: string,
  onUrlUpdate?: (update: { queryString: string }) => void,
) {
  return render(
    <NuqsTestingAdapter searchParams={searchParams} onUrlUpdate={onUrlUpdate}>
      <ContactRecordView id="c-100" />
    </NuqsTestingAdapter>,
  );
}

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
  mockCanWrite = true;
  mockCanReadOrders = true;
});

describe("ContactRecordView orders tab", () => {
  it("shows the orders tab when the user can read orders", () => {
    renderRecord();
    expect(
      screen.getByRole("tab", { name: "record.sections.orders" }),
    ).toBeInTheDocument();
  });

  it("hides the orders tab without the orders permission", () => {
    mockCanReadOrders = false;
    renderRecord();
    expect(
      screen.queryByRole("tab", { name: "record.sections.orders" }),
    ).not.toBeInTheDocument();
  });
});

describe("ContactRecordView tab in the URL", () => {
  it("opens the tab named in the URL on a deep link", () => {
    renderRecord("?tab=orders");
    expect(screen.getByText("orders panel")).toBeDefined();
    expect(
      screen
        .getByRole("tab", { name: "record.sections.orders" })
        .getAttribute("aria-selected"),
    ).toBe("true");
  });

  it("stores the selected tab and clears the orders params when leaving it", async () => {
    const updates: string[] = [];
    renderRecord(
      "?tab=orders&sort=amount&order=asc&after=abc&orderedFrom=2026-01-01",
      ({ queryString }) => updates.push(queryString),
    );

    fireEvent.click(
      screen.getByRole("tab", { name: "record.sections.overview" }),
    );

    await waitFor(() => {
      const last = updates[updates.length - 1] ?? "";
      expect(last).not.toContain("sort=");
      expect(last).not.toContain("after=");
      expect(last).not.toContain("orderedFrom=");
    });
  });
});

describe("ContactRecordView inline edit and soft delete", () => {
  it("inline edit sends expected version token when saving", async () => {
    mockUpdateContact.mockResolvedValue({ id: "c-100", version: 43 });

    renderRecord();

    // Click on first name inline edit trigger
    const firstNameBtn = screen.getByRole("button", {
      name: /record\.fields\.firstName/i,
    });
    fireEvent.click(firstNameBtn);

    const input = screen.getByRole("textbox", {
      name: /record\.fields\.firstName/i,
    });
    fireEvent.change(input, { target: { value: "Jonathan" } });
    fireEvent.keyDown(input, { key: "Enter", code: "Enter" });

    await waitFor(() => {
      expect(mockUpdateContact).toHaveBeenCalledWith(
        "c-100",
        expect.objectContaining({
          version: 42,
          firstName: "Jonathan",
        }),
      );
    });
  });

  it("soft delete triggers delete mutation, navigates, and shows undo toast", async () => {
    mockDeleteContact.mockResolvedValue(undefined);

    renderRecord();

    const deleteBtn = screen.getByRole("button", { name: "record.delete" });
    fireEvent.click(deleteBtn);

    // ConfirmDialog opens
    const dialog = screen.getByRole("alertdialog");
    const confirmBtn = within(dialog).getByRole("button", {
      name: "record.delete",
    });
    fireEvent.click(confirmBtn);

    await waitFor(() => {
      expect(mockDeleteContact).toHaveBeenCalledWith("c-100");
      expect(mockRouterPush).toHaveBeenCalledWith("/app/contacts");
      expect(mockUndoToast).toHaveBeenCalled();
    });
  });

  it("omits archived custom fields from submitted payload when saving an active custom field", async () => {
    mockUpdateContact.mockResolvedValue({ id: "c-100", version: 43 });

    renderRecord();

    // Active field inline edit
    const activeFieldBtn = screen.getByRole("button", {
      name: /Active Field/i,
    });
    fireEvent.click(activeFieldBtn);

    const input = screen.getByRole("textbox", {
      name: /Active Field/i,
    });
    fireEvent.change(input, { target: { value: "new_value" } });
    fireEvent.keyDown(input, { key: "Enter", code: "Enter" });

    await waitFor(() => {
      expect(mockUpdateContact).toHaveBeenCalledWith(
        "c-100",
        expect.objectContaining({
          customFields: {
            active_key: "new_value",
            // archived_key MUST NOT be in submitted customFields
          },
        }),
      );
    });

    const callArgs = mockUpdateContact.mock.calls[0][1];
    expect(callArgs.customFields).not.toHaveProperty("archived_key");
  });

  it("renders plain text without edit buttons when user lacks write permission", () => {
    mockCanWrite = false;

    renderRecord();

    // No edit buttons for first name or email
    expect(
      screen.queryByRole("button", { name: /record\.fields\.firstName/i }),
    ).toBeNull();
    expect(
      screen.queryByRole("button", { name: /record\.fields\.email/i }),
    ).toBeNull();

    // Plain text should still be displayed
    expect(screen.getByText("John")).toBeInTheDocument();
    expect(screen.getByText("john@example.com")).toBeInTheDocument();

    // Tag detach button should not be rendered
    expect(screen.queryByRole("button", { name: /detach/i })).toBeNull();
  });
});
