import { describe, expect, it } from "vitest";

import { localeAlternates } from "./landing-metadata";

describe("locale alternates", () => {
  it("point every locale and x-default at the same page", () => {
    expect(localeAlternates("en", "privacy")).toEqual({
      canonical: "/en/privacy",
      languages: {
        uk: "/uk/privacy",
        en: "/en/privacy",
        "x-default": "/uk/privacy",
      },
    });
    expect(localeAlternates("uk", "home").canonical).toBe("/uk");
  });
});
