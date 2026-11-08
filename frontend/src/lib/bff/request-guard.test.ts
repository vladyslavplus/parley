import { describe, expect, it } from "vitest";

import { ERROR_CODES } from "@/constants/error-codes";
import { HEADERS } from "@/constants/session";

import { rejectUnsafeRequest } from "./request-guard";

const URL_BASE = "http://localhost:3000/api/auth/login";
const JSON_TYPE = { [HEADERS.contentType]: "application/json" };

function post(headers: Record<string, string>, body: string | null = "{}") {
  return new Request(URL_BASE, { method: "POST", headers, body });
}

describe("rejectUnsafeRequest", () => {
  it("rejects a cross-site fetch", async () => {
    const response = rejectUnsafeRequest(
      post({ ...JSON_TYPE, [HEADERS.secFetchSite]: "cross-site" }),
    );
    expect(response?.status).toBe(403);
    expect(await response?.json()).toMatchObject({
      code: ERROR_CODES.requestRejected,
    });
  });

  it("falls back to Origin when Sec-Fetch-Site is absent", () => {
    expect(
      rejectUnsafeRequest(
        post({ ...JSON_TYPE, [HEADERS.origin]: "https://evil.example" }),
      )?.status,
    ).toBe(403);
    expect(
      rejectUnsafeRequest(
        post({ ...JSON_TYPE, [HEADERS.origin]: "http://localhost:3000" }),
      ),
    ).toBeNull();
  });

  it("rejects a body that is not JSON", () => {
    const response = rejectUnsafeRequest(
      post({
        [HEADERS.secFetchSite]: "same-origin",
        [HEADERS.contentType]: "text/plain;charset=UTF-8",
      }),
    );
    expect(response?.status).toBe(403);
  });

  it("accepts a same-origin JSON body and a same-origin empty post", () => {
    expect(
      rejectUnsafeRequest(
        post({
          [HEADERS.secFetchSite]: "same-origin",
          [HEADERS.contentType]: "application/json; charset=utf-8",
        }),
      ),
    ).toBeNull();
    expect(
      rejectUnsafeRequest(
        post({ [HEADERS.secFetchSite]: "same-origin" }, null),
      ),
    ).toBeNull();
  });

  it("leaves safe methods alone", () => {
    expect(
      rejectUnsafeRequest(new Request(URL_BASE, { method: "GET" })),
    ).toBeNull();
  });
});
