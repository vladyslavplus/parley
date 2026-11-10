import { describe, expect, it } from "vitest";

import { recipientEmail } from "@/features/campaigns/recipients/recipient-email";

describe("recipient email", () => {
  it("shows the address when the caller may see it", () => {
    expect(
      recipientEmail({ emailVisible: true, email: "client01@example.test" }),
    ).toEqual({ kind: "address", address: "client01@example.test" });
  });

  it("reads a hidden address from the visibility flag, not from the null", () => {
    expect(recipientEmail({ emailVisible: false, email: null })).toEqual({
      kind: "hidden",
    });
  });

  it("treats a visible but empty address as no address", () => {
    expect(recipientEmail({ emailVisible: true, email: null })).toEqual({
      kind: "none",
    });
  });
});
