import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { HEADERS } from "@/constants/session";

import { backendFetch, clientAddressOf } from "./server";

function incoming(forwardedFor?: string) {
  return new Request("http://localhost/api/auth/login", {
    method: "POST",
    headers: forwardedFor ? { [HEADERS.forwardedFor]: forwardedFor } : {},
  });
}

function sentHeaders(fetchMock: ReturnType<typeof vi.fn>): Headers {
  const init = (fetchMock.mock.calls[0] as unknown as [string, RequestInit])[1];
  return new Headers(init.headers);
}

beforeEach(() => {
  vi.stubEnv("API_BASE_URL", "http://backend.test");
});

afterEach(() => {
  vi.unstubAllGlobals();
  vi.unstubAllEnvs();
});

describe("backendFetch", () => {
  it("sends the resolved end-user address to the backend", async () => {
    const fetchMock = vi.fn(async () => new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchMock);

    await backendFetch({
      method: "POST",
      path: "/auth/login",
      body: "{}",
      clientAddress: "203.0.113.7",
    });

    expect(sentHeaders(fetchMock).get(HEADERS.forwardedFor)).toBe(
      "203.0.113.7",
    );
  });

  it("sends no forwarded address when none was resolved", async () => {
    const fetchMock = vi.fn(async () => new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchMock);

    await backendFetch({ method: "POST", path: "/auth/login", body: "{}" });

    expect(sentHeaders(fetchMock).has(HEADERS.forwardedFor)).toBe(false);
  });
});

describe("clientAddressOf", () => {
  it("ignores a client-supplied header without a trusted ingress", () => {
    expect(clientAddressOf(incoming("198.51.100.9"))).toBeNull();
  });

  it("takes the last entry, appended by the ingress, when trusted", () => {
    vi.stubEnv("TRUSTED_INGRESS", "true");
    expect(clientAddressOf(incoming("6.6.6.6, 203.0.113.7"))).toBe(
      "203.0.113.7",
    );
  });

  it("drops a last entry that is not an IP address", () => {
    vi.stubEnv("TRUSTED_INGRESS", "true");
    expect(clientAddressOf(incoming("203.0.113.7, not-an-ip"))).toBeNull();
    expect(clientAddressOf(incoming())).toBeNull();
  });
});
