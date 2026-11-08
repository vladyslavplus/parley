"use client";

import type { Table } from "@tanstack/react-table";
import { createContext, useContext, type ReactNode } from "react";
import type { TableDensity } from "./types";

interface DataTableContextValue<TData = unknown> {
  table: Table<TData>;
  density: TableDensity;
  setDensity: (d: TableDensity) => void;
  searchValue: string;
  onSearchChange: (value: string) => void;
}

const DataTableContext = createContext<DataTableContextValue | null>(null);

export function DataTableProvider<TData>({
  value,
  children,
}: {
  value: DataTableContextValue<TData>;
  children: ReactNode;
}) {
  return (
    <DataTableContext.Provider
      value={value as unknown as DataTableContextValue}
    >
      {children}
    </DataTableContext.Provider>
  );
}

export function useDataTableContext<
  TData = unknown,
>(): DataTableContextValue<TData> {
  const ctx = useContext(DataTableContext);
  if (!ctx) {
    throw new Error("useDataTableContext must be used within a DataTable");
  }
  return ctx as DataTableContextValue<TData>;
}
