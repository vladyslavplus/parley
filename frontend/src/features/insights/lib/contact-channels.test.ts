import { describe, expect, it } from "vitest";

import { ACTION_CODES } from "./codes";
import { channelsForAction, mailtoHref, telHref } from "./contact-channels";

describe("channelsForAction", () => {
  it("offers the phone first for a call and the email first for outreach", () => {
    expect(channelsForAction(ACTION_CODES.call)).toEqual(["phone", "email"]);
    expect(channelsForAction(ACTION_CODES.winBack)).toEqual(["email", "phone"]);
    expect(channelsForAction(ACTION_CODES.nurture)).toEqual(["email", "phone"]);
  });

  it("offers nothing to a deal review, a hold or an unknown code", () => {
    expect(channelsForAction(ACTION_CODES.reviewDeal)).toEqual([]);
    expect(channelsForAction(ACTION_CODES.hold)).toEqual([]);
    expect(channelsForAction("action.future")).toEqual([]);
  });
});

describe("contact links", () => {
  it("keeps the digits and the leading plus of a phone number", () => {
    expect(telHref("+380 (50) 111-22-33")).toBe("tel:+380501112233");
    expect(telHref("n/a")).toBeNull();
  });

  it("builds a mailto link from a trimmed address", () => {
    expect(mailtoHref(" john@example.com ")).toBe("mailto:john@example.com");
    expect(mailtoHref("  ")).toBeNull();
  });
});
