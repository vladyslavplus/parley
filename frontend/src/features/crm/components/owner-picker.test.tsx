import {
  cleanup,
  fireEvent,
  render,
  screen,
  waitFor,
} from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string, params?: Record<string, unknown>) => {
    if (params?.owner) return `${key}: ${params.owner}`;
    if (params?.count !== undefined) return `${key} (${params.count})`;
    return key;
  },
}));

const mockOwners = [
  {
    id: "u-1",
    displayName: "Olena Koval",
    email: "olena@parley.io",
    isActive: true,
  },
  {
    id: "u-2",
    displayName: "Taras Shevchenko",
    email: "taras@parley.io",
    isActive: true,
  },
];

let lastQuery = "";
vi.mock("@/features/crm/hooks/use-owners", () => ({
  useOwners: (params?: { q?: string }) => {
    lastQuery = params?.q ?? "";
    const filtered = lastQuery
      ? mockOwners.filter(
          (o) =>
            o.displayName.toLowerCase().includes(lastQuery.toLowerCase()) ||
            o.email.toLowerCase().includes(lastQuery.toLowerCase()),
        )
      : mockOwners;
    return {
      isFetching: false,
      data: { items: filtered },
    };
  },
}));

const { OwnerPicker } = await import("./owner-picker");

afterEach(() => {
  cleanup();
  lastQuery = "";
});

describe("OwnerPicker", () => {
  it("opens searchable combobox with 5-row cap, displays name + secondary email, and supports selection and clear", async () => {
    const onChange = vi.fn();
    render(<OwnerPicker variant="control" value={null} onChange={onChange} />);

    const trigger = screen.getByRole("button", { name: "owner" });
    expect(trigger).toHaveTextContent("allOwners");
    fireEvent.click(trigger);

    const searchInput = screen.getByRole("combobox");
    expect(searchInput).toBeInTheDocument();

    const listbox = screen.getByRole("listbox");
    expect(listbox).toHaveAttribute("data-max-visible-rows", "5");

    // Primary name and secondary email are both shown
    expect(screen.getByText("Olena Koval")).toBeInTheDocument();
    expect(screen.getByText("olena@parley.io")).toBeInTheDocument();

    // Keyboard navigation: ArrowDown to Olena, Enter to select
    fireEvent.keyDown(searchInput, { key: "ArrowDown" });
    fireEvent.keyDown(searchInput, { key: "Enter" });

    await waitFor(() => {
      expect(onChange).toHaveBeenCalledWith(
        "u-1",
        expect.objectContaining({ id: "u-1", displayName: "Olena Koval" }),
      );
    });
  });

  it("never shows the clear label when an owner filter is active but not in the first search page", () => {
    const onChange = vi.fn();
    render(
      <OwnerPicker
        variant="control"
        value="u-missing-from-page-1"
        onChange={onChange}
      />,
    );

    const trigger = screen.getByRole("button", { name: "owner" });
    expect(trigger).not.toHaveTextContent("allOwners");
    expect(trigger).toHaveTextContent("ownerPicker.selectedFallback");
  });

  it("resolves active owner name from valueDisplayName when owner is not in the first page", () => {
    const onChange = vi.fn();
    render(
      <OwnerPicker
        variant="control"
        value="u-99"
        valueDisplayName="Dmytro Bondar"
        valueEmail="dmytro@parley.io"
        onChange={onChange}
      />,
    );

    const trigger = screen.getByRole("button", { name: "owner" });
    expect(trigger).not.toHaveTextContent("allOwners");
    expect(trigger).toHaveTextContent("Dmytro Bondar");
    expect(trigger).toHaveTextContent("(dmytro@parley.io)");
  });
});
