import { describe, expect, it } from "vitest";

import { sanitizeReturnUrl } from "./return-url";

describe("sanitizeReturnUrl", () => {
  it.each([
    "//evil.com",
    "https://evil.com",
    "http://evil.com/app",
    "/\\evil.com",
    "\\\\evil.com",
    "/%5Cevil.com",
    "%2F%2Fevil.com",
    "/%2F/evil.com",
    "%252F%252Fevil.com",
    "javascript:alert(1)",
    "evil.com",
    "/\t/evil.com",
    "/%09/evil.com",
    "/%0d%0aSet-Cookie:x=1",
    "",
  ])("rejects %j", (value) => {
    expect(sanitizeReturnUrl(value)).toBeNull();
  });

  it("rejects missing values", () => {
    expect(sanitizeReturnUrl(null)).toBeNull();
    expect(sanitizeReturnUrl(undefined)).toBeNull();
  });

  it("keeps same-origin relative paths with query and hash", () => {
    expect(sanitizeReturnUrl("/uk/app/settings/members?x=1#top")).toBe(
      "/uk/app/settings/members?x=1#top",
    );
  });
});
