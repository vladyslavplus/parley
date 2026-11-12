import { cleanup, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/i18n/navigation", () => ({
  Link: ({
    children,
    href,
    className,
  }: {
    children: React.ReactNode;
    href: string;
    className?: string;
  }) => (
    <a href={href} className={className}>
      {children}
    </a>
  ),
}));

vi.mock("@/features/session/components/can", () => ({
  Can: ({ children }: { children: React.ReactNode }) => children,
}));

vi.mock("@/features/crm/hooks/use-custom-fields", () => ({
  useCustomFields: () => ({ data: [] }),
}));

vi.mock("@/features/crm/hooks/use-tags", () => ({
  useTags: () => ({
    data: { items: [{ id: "t-1", name: "VIP", colorIndex: 0 }] },
  }),
}));

vi.mock("@/features/crm/hooks/use-crm-mutations", () => ({
  useCrmMutations: () => ({
    deleteContact: vi.fn(),
    restoreContact: vi.fn(),
  }),
}));

vi.mock("@/features/crm/hooks/use-soft-delete-with-undo", () => ({
  useSoftDeleteWithUndo: () => ({ handleDelete: vi.fn() }),
}));

const { ContactPreviewDrawer } = await import("./contact-preview-drawer");

afterEach(cleanup);

describe("ContactPreviewDrawer", () => {
  it("renders grouped labelled pairs, owner name + secondary email, and shortened single-line footer buttons", () => {
    render(
      <ContactPreviewDrawer
        open
        onOpenChange={vi.fn()}
        contact={{
          id: "c-1",
          firstName: "Iryna",
          lastName: "Melnyk",
          email: "iryna@example.com",
          phone: null,
          jobTitle: "VP Sales",
          companyId: "comp-1",
          companyName: "Acme Corp",
          ownerUserId: "u-1",
          ownerDisplayName: "Dmytro Bondar",
          ownerEmail: "dmytro@parley.io",
          country: "UA",
          city: "Kyiv",
          tagIds: ["t-1"],
          customFields: {},
          segment: null,
          createdAt: "2026-02-01T10:00:00Z",
          updatedAt: "2026-02-01T10:00:00Z",
          version: 1,
        }}
      />,
    );

    // Header caption "Name" above title
    expect(screen.getByText("columns.name")).toBeInTheDocument();
    expect(screen.getByText("Iryna Melnyk")).toBeInTheDocument();

    // Group headings
    expect(
      screen.getByText("preview.groups.contactDetails"),
    ).toBeInTheDocument();
    expect(screen.getByText("preview.groups.ownership")).toBeInTheDocument();
    expect(screen.getByText("preview.groups.details")).toBeInTheDocument();

    // Owner shows displayName primary and email secondary
    expect(screen.getByText("Dmytro Bondar")).toBeInTheDocument();
    expect(screen.getByText("dmytro@parley.io")).toBeInTheDocument();

    // Phone has no value so its label is hidden
    expect(screen.queryByText("record.fields.phone")).toBeNull();

    // Shortened single-line footer buttons
    const deleteBtn = screen.getByRole("button", { name: /preview\.delete/i });
    const backBtn = screen.getByRole("button", { name: "preview.back" });
    expect(deleteBtn.className).toContain("whitespace-nowrap");
    expect(backBtn.className).toContain("whitespace-nowrap");
  });
});
