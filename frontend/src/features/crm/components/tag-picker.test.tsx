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
    if (params?.tag) return `${key}: ${params.tag}`;
    if (params?.count !== undefined) return `${key} (${params.count})`;
    return key;
  },
}));

const mockTags = [
  { id: "t-1", name: "VIP", colorIndex: 0 },
  { id: "t-2", name: "Lead", colorIndex: 1 },
];

vi.mock("@/features/crm/hooks/use-tags", () => ({
  useTags: () => ({
    isFetching: false,
    data: { items: mockTags, nextCursor: null },
  }),
}));

const { TagPicker } = await import("./tag-picker");

afterEach(cleanup);

describe("TagPicker", () => {
  it("opens searchable combobox, shows helper text explaining tags, caps visible rows at 5, and selects tag", async () => {
    const onChange = vi.fn();
    render(<TagPicker value={null} onChange={onChange} />);

    fireEvent.click(screen.getByRole("button", { name: "tag" }));

    expect(screen.getByRole("combobox")).toBeInTheDocument();
    expect(screen.getByText("tagPicker.help")).toBeInTheDocument();
    expect(screen.getByRole("listbox")).toHaveAttribute(
      "data-max-visible-rows",
      "5",
    );

    fireEvent.click(screen.getByText("VIP"));

    await waitFor(() => {
      expect(onChange).toHaveBeenCalledWith(
        "t-1",
        expect.objectContaining({ id: "t-1", name: "VIP" }),
      );
    });
  });

  it("never shows allTags when a tag filter is active but not in the first page", () => {
    const onChange = vi.fn();
    render(<TagPicker value="t-missing-from-page-1" onChange={onChange} />);

    const trigger = screen.getByRole("button", { name: "tag" });
    expect(trigger).not.toHaveTextContent("allTags");
    expect(trigger).toHaveTextContent("tagPicker.selectedFallback");
  });
});
