import { NextRequest } from "next/server";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { HEADERS, SESSION_COOKIES } from "@/constants/session";

const refreshSession = vi.fn();
vi.mock("@/lib/session/refresh", () => ({ refreshSession }));

const route = await import("./route");

const ID = "3f2b8c1e-7d4a-4c1f-9b6e-0a1b2c3d4e5f";
const tokens = {
  accessToken: "access-2",
  accessTokenExpiresAt: new Date(Date.now() + 60_000).toISOString(),
  refreshToken: "refresh-2",
  refreshTokenExpiresAt: new Date(Date.now() + 120_000).toISOString(),
};

function call(
  method: string,
  path: string[],
  options: { headers?: Record<string, string>; body?: string } = {},
) {
  const request = new NextRequest(
    `http://localhost/api/backend/${path.join("/")}`,
    {
      method,
      body: options.body,
      headers: {
        [HEADERS.secFetchSite]: "same-origin",
        [HEADERS.contentType]: "application/json",
        cookie: `${SESSION_COOKIES.access}=access-1; ${SESSION_COOKIES.refresh}=refresh-1`,
        ...options.headers,
      },
    },
  );
  const handler = route[method as "GET" | "POST" | "PUT" | "DELETE"];
  return handler(request, { params: Promise.resolve({ path }) });
}

let fetchMock: ReturnType<typeof vi.fn>;

beforeEach(() => {
  vi.stubEnv("API_BASE_URL", "http://backend.test");
  refreshSession.mockReset();
  fetchMock = vi.fn(async () => Response.json({ id: ID }));
  vi.stubGlobal("fetch", fetchMock);
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});

function upstreamHeaders(index = 0): Headers {
  const init = (
    fetchMock.mock.calls[index] as unknown as [string, RequestInit]
  )[1];
  return new Headers(init.headers);
}

describe("backend proxy allow-list", () => {
  it("passes an allowed request with the cookie token as bearer", async () => {
    const response = await call("GET", ["me"]);
    expect(response.status).toBe(200);
    expect(await response.json()).toEqual({ id: ID });
    expect(upstreamHeaders().get(HEADERS.authorization)).toBe(
      "Bearer access-1",
    );
  });

  it("answers 404 for invitation accept, which returns a token pair", async () => {
    fetchMock.mockResolvedValue(Response.json(tokens));
    const response = await call("POST", ["invitations", "accept"], {
      body: "{}",
    });
    expect(response.status).toBe(404);
    expect(fetchMock).not.toHaveBeenCalled();
    expect(await response.text()).not.toContain("accessToken");
  });

  it("answers 404 for organization switch", async () => {
    const response = await call("POST", ["organizations", ID, "switch"], {
      body: "{}",
    });
    expect(response.status).toBe(404);
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("answers 404 for dot segments, plain or encoded", async () => {
    for (const path of [
      ["me", "..", "auth", "login"],
      ["%2e%2e", "me"],
      ["members", "%2E%2E", "auth", "refresh"],
    ]) {
      expect((await call("GET", path)).status).toBe(404);
    }
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("answers 405 for a method the path does not allow", async () => {
    const response = await call("DELETE", ["me"]);
    expect(response.status).toBe(405);
    expect(response.headers.get(HEADERS.allow)).toBe("GET");
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it("rejects a cross-site mutation before calling the backend", async () => {
    const response = await call("POST", ["teams"], {
      body: "{}",
      headers: { [HEADERS.secFetchSite]: "cross-site" },
    });
    expect(response.status).toBe(403);
    expect(fetchMock).not.toHaveBeenCalled();
  });
});

describe("backend proxy session handling", () => {
  it("refreshes once on 401, retries and writes the new cookies", async () => {
    fetchMock
      .mockResolvedValueOnce(new Response(null, { status: 401 }))
      .mockResolvedValueOnce(Response.json({ id: ID }));
    refreshSession.mockResolvedValue({ kind: "ok", tokens });

    const response = await call("GET", ["me"]);

    expect(response.status).toBe(200);
    expect(refreshSession).toHaveBeenCalledTimes(1);
    expect(refreshSession.mock.calls[0]?.[0]).toBe("refresh-1");
    expect(upstreamHeaders(1).get(HEADERS.authorization)).toBe(
      "Bearer access-2",
    );
    const cookies = response.headers.getSetCookie().join("\n");
    expect(cookies).toContain(`${SESSION_COOKIES.access}=access-2`);
    expect(cookies).toContain(`${SESSION_COOKIES.refresh}=refresh-2`);
  });

  it("clears the session when the refresh token is rejected", async () => {
    fetchMock.mockResolvedValue(new Response(null, { status: 401 }));
    refreshSession.mockResolvedValue({ kind: "rejected" });

    const response = await call("GET", ["me"]);

    expect(response.status).toBe(401);
    expect(response.headers.getSetCookie().join("\n")).toMatch(
      /Max-Age=0|Expires=/i,
    );
  });

  it("does not forward the client Authorization or Cookie headers", async () => {
    await call("GET", ["me"], {
      headers: { [HEADERS.authorization]: "Bearer attacker", "x-extra": "1" },
    });
    const headers = upstreamHeaders();
    expect(headers.get(HEADERS.authorization)).toBe("Bearer access-1");
    expect(headers.has(HEADERS.cookie)).toBe(false);
    expect(headers.has("x-extra")).toBe(false);
  });

  it("returns only the safe response headers, including Retry-After", async () => {
    fetchMock.mockResolvedValue(
      new Response("{}", {
        status: 429,
        headers: {
          [HEADERS.contentType]: "application/problem+json",
          [HEADERS.retryAfter]: "30",
          "set-cookie": "backend=1",
          "x-backend-secret": "s",
        },
      }),
    );
    const response = await call("GET", ["me"]);
    expect(response.status).toBe(429);
    expect(response.headers.get(HEADERS.retryAfter)).toBe("30");
    expect(response.headers.get(HEADERS.contentType)).toBe(
      "application/problem+json",
    );
    expect(response.headers.has("x-backend-secret")).toBe(false);
    expect(response.headers.getSetCookie().join("\n")).not.toContain(
      "backend=1",
    );
  });

  it("streams raw binary bytes and preserves UTF-8 BOM unchanged", async () => {
    const bomBytes = new Uint8Array([0xef, 0xbb, 0xbf, 0x61, 0x62, 0x63]);
    fetchMock.mockResolvedValue(
      new Response(bomBytes, {
        status: 200,
        headers: {
          [HEADERS.contentType]: "text/csv; charset=utf-8",
          [HEADERS.contentDisposition]: 'attachment; filename="contacts.csv"',
          [HEADERS.contentLength]: String(bomBytes.length),
        },
      }),
    );

    const response = await call("GET", ["contacts", "exports", ID, "download"]);
    expect(response.status).toBe(200);
    expect(response.headers.get(HEADERS.contentDisposition)).toBe(
      'attachment; filename="contacts.csv"',
    );
    expect(response.headers.get(HEADERS.contentLength)).toBe(
      String(bomBytes.length),
    );

    const arrayBuffer = await response.arrayBuffer();
    const resultBytes = new Uint8Array(arrayBuffer);
    expect(Array.from(resultBytes)).toEqual([
      0xef, 0xbb, 0xbf, 0x61, 0x62, 0x63,
    ]);
  });
});
