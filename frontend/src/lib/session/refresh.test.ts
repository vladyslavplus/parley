import { describe, expect, it, vi } from "vitest";

import {
  createSingleFlight,
  type RefreshExecutor,
  type RefreshOutcome,
} from "./refresh";

const tokens = {
  accessToken: "a2",
  accessTokenExpiresAt: "2030-01-01T00:00:00Z",
  refreshToken: "r2",
  refreshTokenExpiresAt: "2030-02-01T00:00:00Z",
};

describe("createSingleFlight", () => {
  it("calls the backend once for concurrent requests with an expired token", async () => {
    const execute = vi.fn<RefreshExecutor>(
      () =>
        new Promise<RefreshOutcome>((resolve) =>
          setTimeout(() => resolve({ kind: "ok", tokens }), 10),
        ),
    );
    const refresh = createSingleFlight(execute, 1000);

    const [first, second] = await Promise.all([refresh("r1"), refresh("r1")]);

    expect(execute).toHaveBeenCalledTimes(1);
    expect(first).toEqual({ kind: "ok", tokens });
    expect(second).toBe(first);
  });

  it("serves a request carrying the previous token from the shared result", async () => {
    const execute = vi.fn<RefreshExecutor>(async () => ({
      kind: "ok",
      tokens,
    }));
    const refresh = createSingleFlight(execute, 1000);

    await refresh("r1");
    const late = await refresh("r1");

    expect(late.kind).toBe("ok");
    expect(execute).toHaveBeenCalledTimes(1);
  });

  it("does not cache an unavailable backend", async () => {
    const execute = vi
      .fn<RefreshExecutor>()
      .mockResolvedValueOnce({ kind: "unavailable" })
      .mockResolvedValueOnce({ kind: "ok", tokens });
    const refresh = createSingleFlight(execute, 1000);

    expect((await refresh("r1")).kind).toBe("unavailable");
    await new Promise((resolve) => setTimeout(resolve, 5));
    expect((await refresh("r1")).kind).toBe("ok");
    expect(execute).toHaveBeenCalledTimes(2);
  });

  it("keeps separate flights for different tokens", async () => {
    const execute = vi.fn<RefreshExecutor>(async () => ({
      kind: "ok",
      tokens,
    }));
    const refresh = createSingleFlight(execute, 1000);

    await Promise.all([refresh("r1"), refresh("other")]);

    expect(execute).toHaveBeenCalledTimes(2);
  });
});
