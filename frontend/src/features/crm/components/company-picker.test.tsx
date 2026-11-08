import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { afterEach, describe, expect, it, vi } from "vitest";

import enCrm from "@/../messages/en/crm.json";
import { CompanyPicker } from "./company-picker";

const mockCompanies = [
  { id: "c1", name: "Horizon Aerospace", domain: "horizonaero.com" },
  { id: "c2", name: "Apex Technologies", domain: "apextech.io" },
];

vi.mock("@/features/crm/hooks/use-companies", () => ({
  useCompanies: (filters: { q?: string }, options?: { enabled?: boolean }) => {
    if (options?.enabled === false) {
      return { data: { items: [] }, isFetching: false };
    }
    const q = filters?.q?.toLowerCase();
    const items = q
      ? mockCompanies.filter((c) => c.name.toLowerCase().includes(q))
      : mockCompanies;
    return { data: { items }, isFetching: false };
  },
}));

function renderPicker(
  props: Partial<Parameters<typeof CompanyPicker>[0]> = {},
) {
  const onChange = vi.fn();
  const result = render(
    <NextIntlClientProvider
      locale="en"
      messages={{ crm: { companies: enCrm.companies } }}
    >
      <CompanyPicker onChange={onChange} {...props} />
    </NextIntlClientProvider>,
  );
  return { ...result, onChange };
}

describe("CompanyPicker Combobox & Keyboard Navigation", () => {
  afterEach(cleanup);

  it("renders trigger button with aria-haspopup=listbox and aria-expanded=false when closed", () => {
    renderPicker();
    const trigger = screen.getByRole("button", { name: /select company/i });
    expect(trigger).toHaveAttribute("aria-expanded", "false");
    expect(trigger).toHaveAttribute("aria-haspopup", "listbox");
  });

  it("opens popover on click and sets up combobox input and listbox with option roles", () => {
    renderPicker();
    const trigger = screen.getByRole("button", { name: /select company/i });
    fireEvent.click(trigger);

    expect(screen.getByRole("combobox")).toBeInTheDocument();
    expect(
      screen.getByRole("listbox", { name: "Companies" }),
    ).toBeInTheDocument();
    const options = screen.getAllByRole("option");
    expect(options).toHaveLength(3); // No company + 2 companies
    expect(options[0]).toHaveTextContent("No company");
    expect(options[1]).toHaveTextContent("Horizon Aerospace");
  });

  it("handles keyboard navigation: ArrowDown, ArrowUp, Enter to select", () => {
    const { onChange } = renderPicker();
    const trigger = screen.getByRole("button", { name: /select company/i });
    fireEvent.click(trigger);

    const input = screen.getByRole("combobox");
    // Initial activeIndex is 0 (No company option)
    fireEvent.keyDown(input, { key: "ArrowDown" }); // activeIndex = 1 (Horizon Aerospace)
    fireEvent.keyDown(input, { key: "Enter" });

    expect(onChange).toHaveBeenCalledWith("c1", "Horizon Aerospace");
  });

  it("closes on Escape key", () => {
    renderPicker();
    const trigger = screen.getByRole("button", { name: /select company/i });
    fireEvent.click(trigger);
    expect(screen.getByRole("listbox")).toBeInTheDocument();

    const input = screen.getByRole("combobox");
    fireEvent.keyDown(input, { key: "Escape" });

    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  });
});
