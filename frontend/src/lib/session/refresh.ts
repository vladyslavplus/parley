import { REFRESH_SHARE_WINDOW_MS } from "@/constants/session";
import { postBackend, type BackendContext } from "@/lib/bff/server";

import { isTokenPair, type TokenPair } from "./tokens";

export type RefreshOutcome =
  | { kind: "ok"; tokens: TokenPair }
  | { kind: "rejected" }
  | { kind: "unavailable" };

export type RefreshExecutor = (
  refreshToken: string,
  context?: BackendContext,
) => Promise<RefreshOutcome>;

export const requestRefresh: RefreshExecutor = async (
  refreshToken,
  context,
) => {
  let response: Response;
  try {
    response = await postBackend("/auth/refresh", {
      body: JSON.stringify({ refreshToken }),
      ...context,
    });
  } catch {
    return { kind: "unavailable" };
  }
  if (response.ok) {
    const body: unknown = await response.json().catch(() => null);
    return isTokenPair(body)
      ? { kind: "ok", tokens: body }
      : { kind: "unavailable" };
  }
  if (response.status === 400 || response.status === 401) {
    return { kind: "rejected" };
  }
  return { kind: "unavailable" };
};

// Concurrent callers with the same refresh token share one backend call. A
// finished result is kept for a short window so a request that still carries
// the previous token reuses it instead of rotating the token again.
export function createSingleFlight(
  execute: RefreshExecutor,
  shareWindowMs: number = REFRESH_SHARE_WINDOW_MS,
) {
  const inflight = new Map<string, Promise<RefreshOutcome>>();

  return (
    refreshToken: string,
    context?: BackendContext,
  ): Promise<RefreshOutcome> => {
    const existing = inflight.get(refreshToken);
    if (existing) return existing;

    const pending = execute(refreshToken, context).then((outcome) => {
      const keepFor = outcome.kind === "unavailable" ? 0 : shareWindowMs;
      setTimeout(() => {
        if (inflight.get(refreshToken) === pending) {
          inflight.delete(refreshToken);
        }
      }, keepFor).unref?.();
      return outcome;
    });
    inflight.set(refreshToken, pending);
    return pending;
  };
}

export const refreshSession = createSingleFlight(requestRefresh);
