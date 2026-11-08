import { NextRequest } from "next/server";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { SESSION_COOKIES } from "@/constants/session";

const refreshSession = vi.fn();
vi.mock("@/lib/session/refresh", () => ({ refreshSession }));

const { proxy } = await import("./proxy");

function request(path: string, cookies: Record<string, string> = {}) {
  const cookie = Object.entries(cookies)
    .map(([key, value]) => `${key}=${value}`)
    .join("; ");
  return new NextRequest(`http://localhost${path}`, {
    headers: cookie ? { cookie } : {},
  });
}

const tokens = {
  accessToken: "a2",
  accessTokenExpiresAt: new Date(Date.now() + 60_000).toISOString(),
  refreshToken: "r2",
  refreshTokenExpiresAt: new Date(Date.now() + 120_000).toISOString(),
};

beforeEach(() => refreshSession.mockReset());

describe("proxy guard", () => {
  it("redirects to login with the original path when there is no session", async () => {
    const response = await proxy(request("/uk/app/settings/members?x=1"));
    const location = new URL(response.headers.get("location") ?? "");
    expect(location.pathname).toBe("/uk/login");
    expect(location.searchParams.get("returnUrl")).toBe(
      "/uk/app/settings/members?x=1",
    );
  });

  it("lets a request with an access cookie through without calling the backend", async () => {
    const response = await proxy(
      request("/uk/app/settings/profile", { [SESSION_COOKIES.access]: "a1" }),
    );
    expect(response.headers.get("location")).toBeNull();
    expect(refreshSession).not.toHaveBeenCalled();
    expect(
      response.headers.get("x-middleware-request-x-next-intl-locale"),
    ).toBe("uk");
  });

  it("refreshes an expired access token and stores the new pair", async () => {
    refreshSession.mockResolvedValue({ kind: "ok", tokens });
    const response = await proxy(
      request("/uk/app/settings/profile", { [SESSION_COOKIES.refresh]: "r1" }),
    );
    expect(response.headers.get("location")).toBeNull();
    expect(response.headers.getSetCookie().join("\n")).toContain(
      `${SESSION_COOKIES.access}=a2`,
    );
  });

  it("signs out only when the refresh token is rejected", async () => {
    refreshSession.mockResolvedValue({ kind: "rejected" });
    const rejected = await proxy(
      request("/en/app", { [SESSION_COOKIES.refresh]: "r1" }),
    );
    expect(new URL(rejected.headers.get("location") ?? "").pathname).toBe(
      "/en/login",
    );

    refreshSession.mockResolvedValue({ kind: "unavailable" });
    const unavailable = await proxy(
      request("/en/app", { [SESSION_COOKIES.refresh]: "r1" }),
    );
    expect(unavailable.headers.get("location")).toBeNull();
  });

  it("does not guard the login page", async () => {
    const response = await proxy(request("/uk/login"));
    expect(
      new URL(response.headers.get("location") ?? "http://x/uk/login").pathname,
    ).not.toContain("returnUrl");
    expect(refreshSession).not.toHaveBeenCalled();
  });
});
