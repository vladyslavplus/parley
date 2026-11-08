import type { ColumnDef } from "@tanstack/react-table";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { NextIntlClientProvider } from "next-intl";
import { NuqsTestingAdapter } from "nuqs/adapters/testing";
import { afterEach, describe, expect, it, vi } from "vitest";

import tableEn from "../../../messages/en/table.json";
import { DataTable } from "./data-table";
import { useTableUrlState } from "./use-table-url-state";

afterEach(cleanup);

interface TestRow {
  id: string;
  name: string;
  value: number;
}

const columns: ColumnDef<TestRow, unknown>[] = [
  {
    id: "select",
    header: "Select",
    cell: ({ row }) => (
      <input
        type="checkbox"
        data-testid={`select-${row.original.id}`}
        checked={row.getIsSelected()}
        onChange={row.getToggleSelectedHandler()}
      />
    ),
  },
  {
    accessorKey: "name",
    header: "Name",
    cell: ({ row }) => <span>{row.original.name}</span>,
  },
  {
    accessorKey: "value",
    header: "Value",
    cell: ({ row }) => <span>{row.original.value}</span>,
  },
];

const testData: TestRow[] = [
  { id: "1", name: "Row One", value: 100 },
  { id: "2", name: "Row Two", value: 200 },
  { id: "3", name: "Row Three", value: 300 },
];

function generateManyRows(count: number): TestRow[] {
  return Array.from({ length: count }, (_, i) => ({
    id: `row-${i + 1}`,
    name: `Item ${String.fromCharCode(65 + (i % 26))}-${(i + 1).toString().padStart(2, "0")}`,
    value: (i + 1) * 10,
  }));
}

function renderTable(
  props: Partial<Parameters<typeof DataTable<TestRow>>[0]> = {},
) {
  return render(
    <NextIntlClientProvider locale="en" messages={{ table: tableEn }}>
      <DataTable
        columns={columns}
        data={testData}
        getRowId={(r) => r.id}
        {...props}
      />
    </NextIntlClientProvider>,
  );
}

function ControlledTableWithUrl({
  data,
  defaultPageSize = 10,
}: {
  data: TestRow[];
  defaultPageSize?: number;
}) {
  const urlState = useTableUrlState({ defaultPageSize });
  return (
    <DataTable
      columns={columns}
      data={data}
      getRowId={(r) => r.id}
      sorting={urlState.sorting}
      onSortingChange={urlState.onSortingChange}
      pagination={urlState.pagination}
      onPaginationChange={urlState.onPaginationChange}
      globalFilter={urlState.search}
      onGlobalFilterChange={urlState.onGlobalFilterChange}
    />
  );
}

describe("DataTable", () => {
  it("renders all rows and columns properly", () => {
    renderTable();
    expect(screen.getByText("Row One")).toBeInTheDocument();
    expect(screen.getByText("Row Two")).toBeInTheDocument();
    expect(screen.getByText("Row Three")).toBeInTheDocument();
  });

  it("handles keyboard navigation: arrows navigate, Enter opens, Space selects", () => {
    const onRowClick = vi.fn();
    renderTable({ onRowClick });

    const tableGrid = screen.getByRole("grid", { name: "Data table" });
    tableGrid.focus();

    // Down arrow to highlight row 0
    fireEvent.keyDown(tableGrid, { key: "ArrowDown" });
    const row0 = screen.getByText("Row One").closest("tr");
    expect(row0).toHaveClass("ring-2");

    // Space toggles selection
    fireEvent.keyDown(tableGrid, { key: " " });
    const checkbox0 = screen.getByTestId("select-1") as HTMLInputElement;
    expect(checkbox0.checked).toBe(true);

    // Enter opens row
    fireEvent.keyDown(tableGrid, { key: "Enter" });
    expect(onRowClick).toHaveBeenCalledWith(testData[0]);

    // Down arrow to row 1
    fireEvent.keyDown(tableGrid, { key: "ArrowDown" });
    const row1 = screen.getByText("Row Two").closest("tr");
    expect(row1).toHaveClass("ring-2");
  });

  it("Space on a cell checkbox selects only that row and does not bubble to row action", () => {
    const onRowClick = vi.fn();
    renderTable({ onRowClick });

    const checkbox = screen.getByTestId("select-2") as HTMLInputElement;
    checkbox.focus();
    fireEvent.keyDown(checkbox, { key: " " });
    fireEvent.click(checkbox);

    expect(checkbox.checked).toBe(true);
    expect(onRowClick).not.toHaveBeenCalled();
    const checkbox1 = screen.getByTestId("select-1") as HTMLInputElement;
    expect(checkbox1.checked).toBe(false);
  });

  it("shows floating bulk actions bar with accurate filtered total count", () => {
    renderTable({
      bulkActions: (selected) => (
        <button type="button">Delete ({selected.length})</button>
      ),
    });

    expect(screen.queryByText(/Delete \(/)).not.toBeInTheDocument();

    const checkbox = screen.getByTestId("select-1");
    fireEvent.click(checkbox);

    expect(screen.getByText("Delete (1)")).toBeInTheDocument();
    expect(
      screen.getAllByText("1 of 3 selected").length,
    ).toBeGreaterThanOrEqual(1);
  });

  it("toggles density between compact and comfortable", () => {
    renderTable();
    const densityBtn = screen.getByTitle("Density");
    expect(screen.getByText("Compact")).toBeInTheDocument();

    fireEvent.click(densityBtn);
    expect(screen.getByText("Comfortable")).toBeInTheDocument();
  });

  it("clicking Next updates to Page 2 of N and enables Previous button (verifying React Compiler does not freeze controls)", () => {
    const manyRows = generateManyRows(30);
    render(
      <NextIntlClientProvider locale="en" messages={{ table: tableEn }}>
        <DataTable
          columns={columns}
          data={manyRows}
          getRowId={(r) => r.id}
          initialPageSize={10}
        />
      </NextIntlClientProvider>,
    );

    expect(screen.getByText("Page 1 of 3")).toBeInTheDocument();
    const prevButton = screen.getByRole("button", { name: "Previous" });
    const nextButton = screen.getByRole("button", { name: "Next" });

    expect(prevButton).toBeDisabled();
    expect(nextButton).not.toBeDisabled();

    // Click Next
    fireEvent.click(nextButton);

    // Page must update to Page 2 of 3 and Previous must be enabled
    expect(screen.getByText("Page 2 of 3")).toBeInTheDocument();
    expect(prevButton).not.toBeDisabled();
    expect(nextButton).not.toBeDisabled();
  });

  it("restores sort and page from URL state (?sort=name&order=desc&page=2)", () => {
    const manyRows = generateManyRows(30);

    render(
      <NuqsTestingAdapter searchParams="?sort=name&order=desc&page=2">
        <NextIntlClientProvider locale="en" messages={{ table: tableEn }}>
          <ControlledTableWithUrl data={manyRows} defaultPageSize={10} />
        </NextIntlClientProvider>
      </NuqsTestingAdapter>,
    );

    // Restores Page 2 of 3
    expect(screen.getByText("Page 2 of 3")).toBeInTheDocument();
    const prevButton = screen.getByRole("button", { name: "Previous" });
    expect(prevButton).not.toBeDisabled();

    // Verify row order for order=desc: Item Z-26 was on page 1, page 2 starts with Item P-16
    expect(screen.queryByText("Item Z-26")).not.toBeInTheDocument();
    expect(screen.getByText("Item P-16")).toBeInTheDocument();
    expect(screen.getByText("Item O-15")).toBeInTheDocument();

    const nameHeader = screen.getByRole("columnheader", { name: /Name/i });
    expect(nameHeader.getAttribute("aria-sort")).toBe("descending");
  });

  it("toggles sorting via column header button and updates aria-sort", () => {
    renderTable();
    const nameHeader = screen.getByRole("columnheader", { name: /Name/i });
    expect(nameHeader.getAttribute("aria-sort")).toBe("none");

    const sortBtn = screen.getByRole("button", { name: /Name/i });
    fireEvent.click(sortBtn);

    // After first click: ascending
    expect(nameHeader.getAttribute("aria-sort")).toBe("ascending");

    // After second click: descending
    fireEvent.click(sortBtn);
    expect(nameHeader.getAttribute("aria-sort")).toBe("descending");
  });
});
