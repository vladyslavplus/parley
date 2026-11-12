import { describe, expect, it } from "vitest";

import { recipientsQuery } from "@/features/campaigns/recipients/recipients-query";

const none = { q: "", arm: "", targeted: "", sendStatus: "", converted: "" };

describe("recipients query", () => {
  it("sends only the paging when no filter is set", () => {
    expect(recipientsQuery(none, { after: "", limit: 20 })).toEqual({
      q: undefined,
      arm: undefined,
      targeted: undefined,
      sendStatus: undefined,
      converted: undefined,
      after: undefined,
      limit: 20,
    });
  });

  it("maps set filters and drops values the endpoint does not accept", () => {
    const query = recipientsQuery(
      {
        q: " Olena ",
        arm: "holdout",
        targeted: "false",
        sendStatus: "retryable_failed",
        converted: "true",
      },
      { after: "cursor-1", limit: 50 },
    );
    expect(query).toMatchObject({
      q: "Olena",
      arm: "holdout",
      targeted: "false",
      sendStatus: "retryable_failed",
      converted: "true",
      after: "cursor-1",
      limit: 50,
    });

    const hand = recipientsQuery(
      { ...none, arm: "everyone", targeted: "yes", sendStatus: "opened" },
      { after: "", limit: 20 },
    );
    expect(hand.arm).toBeUndefined();
    expect(hand.targeted).toBeUndefined();
    expect(hand.sendStatus).toBeUndefined();
  });
});
