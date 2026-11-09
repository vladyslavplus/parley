import { act, renderHook } from "@testing-library/react";
import { withNuqsTestingAdapter } from "nuqs/adapters/testing";
import { describe, expect, it } from "vitest";

import { useDealFilters } from "./use-deal-filters";

describe("useDealFilters", () => {
  it("initializes with default values when searchParams are empty", () => {
    const { result } = renderHook(() => useDealFilters(), {
      wrapper: withNuqsTestingAdapter(),
    });

    expect(result.current.q).toBe("");
    expect(result.current.searchInput).toBe("");
    expect(result.current.ownerUserId).toBe("");
    expect(result.current.companyId).toBe("");
    expect(result.current.status).toBe("open");
    expect(result.current.hasActiveFilters).toBe(false);
  });

  it("parses URL search parameters correctly", () => {
    const { result } = renderHook(() => useDealFilters(), {
      wrapper: withNuqsTestingAdapter({
        searchParams: {
          q: "acme",
          ownerUserId: "user-123",
          companyId: "comp-456",
          status: "won",
        },
      }),
    });

    expect(result.current.q).toBe("acme");
    expect(result.current.searchInput).toBe("acme");
    expect(result.current.ownerUserId).toBe("user-123");
    expect(result.current.companyId).toBe("comp-456");
    expect(result.current.status).toBe("won");
    expect(result.current.hasActiveFilters).toBe(true);
  });

  it("clears all active filters when clearFilters is called", () => {
    const { result } = renderHook(() => useDealFilters(), {
      wrapper: withNuqsTestingAdapter({
        searchParams: {
          q: "filter-val",
          ownerUserId: "owner-1",
          status: "lost",
        },
      }),
    });

    expect(result.current.hasActiveFilters).toBe(true);

    act(() => {
      result.current.clearFilters();
    });

    expect(result.current.searchInput).toBe("");
  });
});
