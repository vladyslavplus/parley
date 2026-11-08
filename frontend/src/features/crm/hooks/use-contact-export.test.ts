import { act, cleanup, renderHook, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const mockBackendJson = vi.fn();

vi.mock("next-intl", () => ({
  useLocale: () => "en",
  useTranslations: () => (key: string) => key,
}));

vi.mock("@/constants/limits", () => ({
  LIMITS: {
    exportPollIntervalMs: 10,
  },
}));

vi.mock("@/lib/api/bff-client", () => ({
  backendJson: (...args: unknown[]) => mockBackendJson(...args),
}));

vi.mock("sonner", () => ({
  toast: {
    error: vi.fn(),
    success: vi.fn(),
  },
}));

const { useContactExport } = await import("./use-contact-export");

describe("useContactExport hook", () => {
  beforeEach(() => {
    mockBackendJson.mockReset();
  });

  afterEach(() => {
    cleanup();
    vi.clearAllMocks();
  });

  it("handles export transition from queued to completed", async () => {
    const futureExpires = new Date(Date.now() + 3600000).toISOString();

    // 1. Initial POST /contacts/exports returns queued
    mockBackendJson.mockResolvedValueOnce({
      id: "exp-1",
      status: "queued",
      rowCount: 0,
      expiresAt: futureExpires,
    });

    // 2. Poll GET /contacts/exports/{id} returns completed
    mockBackendJson.mockResolvedValueOnce({
      id: "exp-1",
      status: "completed",
      rowCount: 15,
      expiresAt: futureExpires,
    });

    const { result } = renderHook(() => useContactExport());

    expect(result.current.state.status).toBe("idle");

    await act(async () => {
      await result.current.startExport({ q: "john" });
    });

    await waitFor(() => {
      expect(result.current.state.status).toBe("completed");
    });

    expect(result.current.state.rowCount).toBe(15);
    expect(result.current.state.downloadUrl).toBe(
      "/api/backend/contacts/exports/exp-1/download",
    );
  });

  it("handles failed export status", async () => {
    const futureExpires = new Date(Date.now() + 3600000).toISOString();

    mockBackendJson.mockResolvedValueOnce({
      id: "exp-2",
      status: "queued",
      rowCount: 0,
      expiresAt: futureExpires,
    });

    // Next poll fails
    mockBackendJson.mockResolvedValueOnce({
      id: "exp-2",
      status: "failed",
      rowCount: 0,
      expiresAt: futureExpires,
    });

    const { result } = renderHook(() => useContactExport());

    await act(async () => {
      await result.current.startExport({});
    });

    await waitFor(() => {
      expect(result.current.state.status).toBe("failed");
    });

    expect(result.current.state.downloadUrl).toBeNull();
  });

  it("handles expired export status", async () => {
    const futureExpires = new Date(Date.now() + 3600000).toISOString();

    mockBackendJson.mockResolvedValueOnce({
      id: "exp-3",
      status: "queued",
      rowCount: 0,
      expiresAt: futureExpires,
    });

    // Next poll returns expired timestamp
    mockBackendJson.mockResolvedValueOnce({
      id: "exp-3",
      status: "running",
      rowCount: 0,
      expiresAt: new Date(Date.now() - 10000).toISOString(),
    });

    const { result } = renderHook(() => useContactExport());

    await act(async () => {
      await result.current.startExport({});
    });

    await waitFor(() => {
      expect(result.current.state.status).toBe("expired");
    });
  });

  it("does not trigger further polls when reset() is called while a GET is pending", async () => {
    let pendingPollResolve: ((val: unknown) => void) | undefined;
    const futureExpires = new Date(Date.now() + 3600000).toISOString();

    mockBackendJson.mockResolvedValueOnce({
      id: "exp-4",
      status: "running",
      rowCount: 0,
      expiresAt: futureExpires,
    });

    // The polling GET request hangs
    mockBackendJson.mockImplementationOnce(
      () =>
        new Promise((resolve) => {
          pendingPollResolve = resolve;
        }),
    );

    const { result } = renderHook(() => useContactExport());

    await act(async () => {
      await result.current.startExport({});
    });

    // Let the poll timer fire and launch the GET request
    await new Promise((r) => setTimeout(r, 20));

    // Reset while poll is pending
    act(() => {
      result.current.reset();
    });

    // Resolve the hanging GET request
    await act(async () => {
      pendingPollResolve?.({
        id: "exp-4",
        status: "running",
        rowCount: 0,
        expiresAt: futureExpires,
      });
    });

    // Wait a tick to ensure no further backend calls were scheduled
    await new Promise((r) => setTimeout(r, 50));

    // mockBackendJson was called twice: 1 POST and 1 pending GET
    expect(mockBackendJson).toHaveBeenCalledTimes(2);
    expect(result.current.state.status).toBe("idle");
  });
});
