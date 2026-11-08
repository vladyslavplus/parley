import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { HEADERS, SESSION_COOKIES } from "@/constants/session";

import {
  createSessionStartHandler,
  logoutHandler,
  switchOrganizationHandler,
} from "./handlers";

const SAME_ORIGIN = {
  [HEADERS.secFetchSite]: "same-origin",
  [HEADERS.contentType]: "application/json",
};

const tokens = {
  accessToken: "access-secret-value",
  accessTokenExpiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
  refreshToken: "refresh-secret-value",
  refreshTokenExpiresAt: new Date(Date.now() + 30 * 86_400_000).toISOString(),
};

function mockBackend(response: Response) {
  const fetchMock = vi.fn(async () => response);
  vi.stubGlobal("fetch", fetchMock);
  return fetchMock;
}

function loginRequest() {
  return new Request("http://localhost/api/auth/login", {
    method: "POST",
    headers: SAME_ORIGIN,
    body: JSON.stringify({ email: "a@b.c", password: "x" }),
  });
}

function setCookies(response: Response): string[] {
  return response.headers.getSetCookie();
}

beforeEach(() => {
  vi.stubEnv("API_BASE_URL", "http://backend.test");
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});

describe("session start handler", () => {
  it("stores both tokens in httpOnly cookies and returns no body", async () => {
    mockBackend(Response.json(tokens));
    const response =
      await createSessionStartHandler("/auth/login")(loginRequest());

    expect(response.status).toBe(204);
    expect(await response.text()).toBe("");
    const cookies = setCookies(response);
    const access = cookies.find((c) =>
      c.startsWith(`${SESSION_COOKIES.access}=`),
    );
    const refresh = cookies.find((c) =>
      c.startsWith(`${SESSION_COOKIES.refresh}=`),
    );
    for (const cookie of [access, refresh]) {
      expect(cookie).toMatch(/HttpOnly/i);
      expect(cookie).toMatch(/SameSite=lax/i);
      expect(cookie).toMatch(/Path=\//i);
      expect(cookie).toMatch(/Max-Age=\d+/i);
    }
    expect(access).not.toMatch(/Secure/i);
  });

  it("marks cookies Secure in production", async () => {
    vi.stubEnv("NODE_ENV", "production");
    mockBackend(Response.json(tokens));
    const response =
      await createSessionStartHandler("/auth/login")(loginRequest());
    expect(setCookies(response)).toHaveLength(2);
    for (const cookie of setCookies(response))
      expect(cookie).toMatch(/Secure/i);
  });

  it("passes backend errors through without cookies", async () => {
    mockBackend(
      Response.json(
        {
          status: 401,
          code: "identity.auth.invalid_credentials",
          detail: "Wrong.",
        },
        { status: 401 },
      ),
    );
    const response =
      await createSessionStartHandler("/auth/login")(loginRequest());

    expect(response.status).toBe(401);
    expect(setCookies(response)).toHaveLength(0);
    expect(await response.json()).toMatchObject({ detail: "Wrong." });
  });
});

describe("logout handler", () => {
  it("revokes the refresh token and clears both cookies", async () => {
    const fetchMock = mockBackend(new Response(null, { status: 204 }));
    const request = new Request("http://localhost/api/auth/logout", {
      method: "POST",
      headers: {
        [HEADERS.secFetchSite]: "same-origin",
        cookie: `${SESSION_COOKIES.refresh}=r-1`,
      },
    });
    const response = await logoutHandler(request);

    expect(response.status).toBe(204);
    const call = fetchMock.mock.calls[0] as unknown as [string, RequestInit];
    expect(call[0]).toBe("http://backend.test/auth/logout");
    expect(call[1].body).toBe(JSON.stringify({ refreshToken: "r-1" }));
    for (const cookie of setCookies(response))
      expect(cookie).toMatch(/Max-Age=0|Expires=/i);
    expect(setCookies(response)).toHaveLength(2);
  });
});

describe("cross-site protection", () => {
  it("does not start a session for a cross-site login post", async () => {
    const fetchMock = mockBackend(Response.json(tokens));
    const request = new Request("http://localhost/api/auth/login", {
      method: "POST",
      headers: {
        [HEADERS.origin]: "https://evil.example",
        [HEADERS.contentType]: "text/plain",
      },
      body: JSON.stringify({ email: "a@b.c", password: "x" }),
    });
    const response = await createSessionStartHandler("/auth/login")(request);

    expect(response.status).toBe(403);
    expect(setCookies(response)).toHaveLength(0);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("passes the backend Retry-After header through on 429", async () => {
    mockBackend(
      Response.json(
        { status: 429, code: "identity.auth.rate_limited" },
        { status: 429, headers: { [HEADERS.retryAfter]: "42" } },
      ),
    );
    const response =
      await createSessionStartHandler("/auth/login")(loginRequest());
    expect(response.status).toBe(429);
    expect(response.headers.get(HEADERS.retryAfter)).toBe("42");
  });
});

describe("switch organization handler", () => {
  const ORG = "3f2b8c1e-7d4a-4c1f-9b6e-0a1b2c3d4e5f";

  function switchRequest(cookie: string) {
    return new Request("http://localhost/api/auth/switch-organization", {
      method: "POST",
      headers: { ...SAME_ORIGIN, cookie },
      body: JSON.stringify({ organizationId: ORG }),
    });
  }

  it("refreshes an expired access token, then switches with the rotated refresh token", async () => {
    const rotated = {
      ...tokens,
      accessToken: "access-rotated",
      refreshToken: "refresh-rotated",
    };
    const switched = {
      ...tokens,
      accessToken: "access-switched",
      refreshToken: "refresh-switched",
    };
    const fetchMock = vi
      .fn()
      .mockResolvedValueOnce(Response.json(rotated))
      .mockResolvedValueOnce(Response.json(switched));
    vi.stubGlobal("fetch", fetchMock);

    const response = await switchOrganizationHandler(
      switchRequest(`${SESSION_COOKIES.refresh}=refresh-old`),
    );

    expect(response.status).toBe(204);
    const [refreshCall, switchCall] = fetchMock.mock.calls as unknown as [
      string,
      RequestInit,
    ][];
    expect(refreshCall?.[0]).toBe("http://backend.test/auth/refresh");
    expect(switchCall?.[0]).toBe(
      `http://backend.test/organizations/${ORG}/switch`,
    );
    expect(
      new Headers(switchCall?.[1].headers).get(HEADERS.authorization),
    ).toBe("Bearer access-rotated");
    expect(switchCall?.[1].body).toBe(
      JSON.stringify({ refreshToken: "refresh-rotated" }),
    );
    expect(setCookies(response).join(";")).toContain(
      `${SESSION_COOKIES.access}=access-switched`,
    );
  });

  it("clears the session cookies when the backend answers 401", async () => {
    mockBackend(new Response(null, { status: 401 }));
    const response = await switchOrganizationHandler(
      switchRequest(
        `${SESSION_COOKIES.access}=a-1; ${SESSION_COOKIES.refresh}=r-1`,
      ),
    );

    expect(response.status).toBe(401);
    expect(setCookies(response)).toHaveLength(2);
    for (const cookie of setCookies(response))
      expect(cookie).toMatch(/Max-Age=0|Expires=/i);
  });
});
