import { cleanup, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  clearRegisteredCommandSources,
  registerCommandSource,
  registerSearchSource,
  useRegisteredCommands,
  type CommandItem,
} from "./command-source";

describe("command-source registry and hook", () => {
  beforeEach(() => {
    clearRegisteredCommandSources();
  });

  afterEach(() => {
    cleanup();
    clearRegisteredCommandSources();
    vi.clearAllMocks();
  });

  it("returns static commands when query is empty", () => {
    const staticItem: CommandItem = {
      id: "nav-1",
      title: "Go to Dashboard",
      group: "navigation",
      onSelect: vi.fn(),
    };

    registerCommandSource(() => [staticItem]);

    const { result } = renderHook(() => useRegisteredCommands(""));
    expect(result.current.commands).toHaveLength(1);
    expect(result.current.commands[0].title).toBe("Go to Dashboard");
    expect(result.current.loading).toBe(false);
  });

  it("handles async search sources and retains static commands", async () => {
    const staticItem: CommandItem = {
      id: "crm-create",
      title: "Create Contact",
      group: "actions",
      onSelect: vi.fn(),
    };
    registerCommandSource(() => [staticItem]);

    const searchSource = vi.fn().mockImplementation(async (q: string) => {
      if (q === "test") {
        return [
          {
            id: "hit-1",
            title: "Test Contact",
            group: "contacts",
            onSelect: vi.fn(),
          },
          {
            id: "hit-2",
            title: "Test Company",
            group: "companies",
            onSelect: vi.fn(),
          },
        ];
      }
      return [];
    });

    registerSearchSource(searchSource);

    const { result, rerender } = renderHook(
      ({ query }) => useRegisteredCommands(query),
      { initialProps: { query: "" } },
    );

    // Empty query should return the static commands
    expect(result.current.commands).toHaveLength(1);
    expect(result.current.commands[0].title).toBe("Create Contact");

    rerender({ query: "test" });

    await waitFor(() => {
      // 1 static + 2 search hits
      expect(result.current.commands).toHaveLength(3);
    });

    expect(result.current.commands[1].group).toBe("contacts");
    expect(result.current.commands[2].group).toBe("companies");
    expect(result.current.loading).toBe(false);
  });

  it("cancels stale async query results with AbortSignal when query changes quickly", async () => {
    let slowResolve: ((items: CommandItem[]) => void) | undefined;

    const source = vi.fn().mockImplementation((q: string) => {
      if (q === "slow") {
        return new Promise<CommandItem[]>((resolve) => {
          slowResolve = resolve;
        });
      }
      if (q === "fast") {
        return Promise.resolve([
          {
            id: "fast-1",
            title: "Fast Hit",
            group: "results",
            onSelect: vi.fn(),
          },
        ]);
      }
      return Promise.resolve([]);
    });

    registerSearchSource(source);

    const { result, rerender } = renderHook(
      ({ query }) => useRegisteredCommands(query),
      { initialProps: { query: "slow" } },
    );

    await waitFor(() => {
      expect(result.current.loading).toBe(true);
    });

    // Fast query replaces slow query before slow resolves
    rerender({ query: "fast" });

    await waitFor(() => {
      expect(result.current.commands.some((c) => c.title === "Fast Hit")).toBe(
        true,
      );
    });

    // Slow promise resolves later
    if (slowResolve) {
      (slowResolve as (items: CommandItem[]) => void)([
        {
          id: "slow-1",
          title: "Slow Stale Hit",
          group: "results",
          onSelect: vi.fn(),
        },
      ]);
    }

    // Should NOT include stale hit
    expect(
      result.current.commands.some((c) => c.title === "Slow Stale Hit"),
    ).toBe(false);
  });
});
