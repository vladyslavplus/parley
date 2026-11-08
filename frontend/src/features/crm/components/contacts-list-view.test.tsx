import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NuqsTestingAdapter } from "nuqs/adapters/testing";
import { afterEach, describe, expect, it, vi } from "vitest";

let mockIsError = false;
const mockRefetch = vi.fn();

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string, params?: Record<string, unknown>) => {
    if (params?.owner) return `${key} ${params.owner}`;
    if (params?.tag) return `${key} ${params.tag}`;
    if (params?.count !== undefined) return `${key} ${params.count}`;
    return key;
  },
}));

vi.mock("@/i18n/navigation", () => ({
  Link: ({ children, href }: { children: React.ReactNode; href: string }) => (
    <a href={href}>{children}</a>
  ),
  useRouter: () => ({ push: vi.fn() }),
}));

vi.mock("@/features/session/components/can", () => ({
  Can: ({ children }: { children: React.ReactNode }) => children,
  useCan: () => true,
}));

const mockContacts = [
  {
    id: "c-1",
    firstName: "Sarah",
    lastName: "Connor",
    email: "sarah@sky.net",
    phone: "+1234567890",
    jobTitle: "Leader",
    companyId: null,
    ownerUserId: "u-1",
    tagIds: [],
    customFields: {},
    createdAt: "2026-01-01T10:00:00Z",
    updatedAt: "2026-01-01T10:00:00Z",
    version: 1,
  },
];

vi.mock("@/features/crm/hooks/use-contacts", () => ({
  useContacts: () => ({
    isPending: false,
    isError: mockIsError,
    refetch: mockRefetch,
    data: mockIsError
      ? undefined
      : {
          items: mockContacts,
          nextCursor: "cursor-token-2",
        },
  }),
}));

vi.mock("@/features/crm/hooks/use-tags", () => ({
  useTags: () => ({ data: { items: [] } }),
}));

vi.mock("@/lib/api/hooks/use-api", () => ({
  useMe: () => ({ data: { user: { id: "u-1" } } }),
  useMembers: () => ({ data: { pages: [{ items: [] }] } }),
}));

vi.mock("@/features/crm/hooks/use-companies", () => ({
  useCompanies: () => ({ data: { items: [] } }),
}));

vi.mock("@/features/crm/hooks/use-crm-mutations", () => ({
  useCrmMutations: () => ({
    bulkAssignContactOwner: vi.fn(),
    bulkDeleteContacts: vi.fn(),
    restoreContact: vi.fn(),
  }),
}));

const { ContactsListView } = await import("./contacts-list-view");

afterEach(() => {
  cleanup();
  mockIsError = false;
});

describe("ContactsListView", () => {
  it("renders contact rows from query data", () => {
    render(
      <NuqsTestingAdapter>
        <ContactsListView />
      </NuqsTestingAdapter>,
    );

    expect(screen.getByText("Sarah Connor")).toBeInTheDocument();
    expect(screen.getByText("sarah@sky.net")).toBeInTheDocument();
    expect(screen.getByText("Leader")).toBeInTheDocument();
  });

  it("opens preview drawer when clicking on a contact row", () => {
    render(
      <NuqsTestingAdapter>
        <ContactsListView />
      </NuqsTestingAdapter>,
    );

    const row = screen.getByText("Sarah Connor").closest("tr");
    expect(row).not.toBeNull();
    fireEvent.click(row as HTMLElement);

    expect(screen.getByText("preview.openRecord")).toBeInTheDocument();
  });

  it("handles error state with preserved toolbar, retry action and conditional reset paging", () => {
    mockIsError = true;

    const { rerender } = render(
      <NuqsTestingAdapter searchParams="?after=bad-cursor">
        <ContactsListView />
      </NuqsTestingAdapter>,
    );

    // Header and toolbar are preserved
    expect(screen.getByText("title")).toBeInTheDocument();
    expect(
      screen.getByPlaceholderText("searchPlaceholder"),
    ).toBeInTheDocument();

    expect(screen.getByText("loadError")).toBeInTheDocument();
    const retryBtn = screen.getByRole("button", { name: /retry/i });
    expect(retryBtn).toBeInTheDocument();
    fireEvent.click(retryBtn);
    expect(mockRefetch).toHaveBeenCalled();

    // With after set, reset paging is offered
    const resetBtn = screen.getByRole("button", { name: /resetPaging/i });
    expect(resetBtn).toBeInTheDocument();
    fireEvent.click(resetBtn);

    // Without after set, reset paging button is hidden
    rerender(
      <NuqsTestingAdapter searchParams="">
        <ContactsListView />
      </NuqsTestingAdapter>,
    );
    expect(screen.queryByRole("button", { name: /resetPaging/i })).toBeNull();
  });
});
