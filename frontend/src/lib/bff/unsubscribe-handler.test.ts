import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { HEADERS } from "@/constants/session";

import { unsubscribeHandler } from "./handlers";

const SAME_ORIGIN = { [HEADERS.secFetchSite]: "same-origin" };

function post(search: string) {
  return new Request(`http://localhost/api/unsubscribe${search}`, {
    method: "POST",
    headers: SAME_ORIGIN,
  });
}

beforeEach(() => {
  vi.stubEnv("API_BASE_URL", "http://backend.test");
});
afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

describe("unsubscribe handler", () => {
  it("forwards the token as an anonymous POST and never sends credentials", async () => {
    const fetchMock = vi.fn(async () => new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchMock);

    const response = await unsubscribeHandler(post("?token=abc%2Bdef"));

    expect(response.status).toBe(204);
    const [url, init] = fetchMock.mock.calls[0] as unknown as [
      string | URL,
      RequestInit,
    ];
    expect(String(url)).toBe("http://backend.test/unsubscribe?token=abc%2Bdef");
    expect(init.method).toBe("POST");
    expect(new Headers(init.headers).has(HEADERS.authorization)).toBe(false);
  });

  it("passes a rate-limit answer through with Retry-After", async () => {
    vi.stubGlobal(
      "fetch",
      vi.fn(async () =>
        Response.json(
          { status: 429 },
          { status: 429, headers: { [HEADERS.retryAfter]: "30" } },
        ),
      ),
    );
    const response = await unsubscribeHandler(post("?token=x"));
    expect(response.status).toBe(429);
    expect(response.headers.get(HEADERS.retryAfter)).toBe("30");
  });

  it("refuses a cross-site request without calling the backend", async () => {
    const fetchMock = vi.fn();
    vi.stubGlobal("fetch", fetchMock);
    const response = await unsubscribeHandler(
      new Request("http://localhost/api/unsubscribe?token=x", {
        method: "POST",
        headers: { [HEADERS.origin]: "https://evil.example" },
      }),
    );
    expect(response.status).toBe(403);
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
