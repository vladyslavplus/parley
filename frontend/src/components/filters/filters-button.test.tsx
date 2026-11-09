import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { User } from "lucide-react";
import { useState } from "react";
import { afterEach, describe, expect, it, vi } from "vitest";

import {
  FilterChips,
  FiltersButton,
} from "@/components/filters/filters-button";
import type { FilterDimension } from "@/components/filters/types";

vi.mock("next-intl", () => ({
  useTranslations: () => (key: string, params?: Record<string, unknown>) =>
    params?.count !== undefined ? `${key} (${params.count})` : key,
}));

const OPTIONS = [
  { id: "u-1", label: "Olena Koval", description: "olena@parley.io" },
  { id: "u-2", label: "Taras Shevchenko", description: null },
];

function Harness({ onChange }: { onChange?: (value: string) => void }) {
  const [value, setValue] = useState("");
  const dimension: FilterDimension = {
    key: "owner",
    label: "Owner",
    icon: User,
    value,
    onChange: (next) => {
      setValue(next);
      onChange?.(next);
    },
    searchPlaceholder: "Search owners",
    emptyLabel: "No owners",
    clearSearchLabel: "Clear search",
    useOptions: () => ({ options: OPTIONS, isFetching: false }),
    useValueLabel: (id) => OPTIONS.find((o) => o.id === id)?.label ?? null,
  };
  return (
    <>
      <FiltersButton dimensions={[dimension]} />
      <FilterChips dimensions={[dimension]} />
    </>
  );
}

describe("FiltersButton", () => {
  afterEach(cleanup);

  it("picks a dimension, selects an option, shows the count badge and a removable chip", () => {
    const onChange = vi.fn();
    render(<Harness onChange={onChange} />);

    fireEvent.click(screen.getByRole("button", { name: "button" }));
    fireEvent.click(screen.getByRole("button", { name: /Owner/ }));
    fireEvent.click(screen.getByRole("option", { name: /Olena Koval/ }));

    expect(onChange).toHaveBeenCalledWith("u-1");
    const trigger = screen.getByRole("button", { name: /button, activeCount/ });
    expect(trigger).toHaveTextContent("1");

    fireEvent.click(screen.getByRole("button", { name: /removeChip/ }));
    expect(onChange).toHaveBeenLastCalledWith("");
    expect(screen.queryByRole("button", { name: /removeChip/ })).toBeNull();
  });
});
