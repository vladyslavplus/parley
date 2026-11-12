import { describe, expect, it } from "vitest";

import { priorityQuery } from "@/features/insights/lib/priority-query";

describe("priority query", () => {
  it("sends only the paging when no segment is chosen", () => {
    expect(priorityQuery({ segment: "" }, { after: "", limit: 20 })).toEqual({
      segment: undefined,
      after: undefined,
      limit: 20,
    });
  });

  it("passes a known segment and the cursor through", () => {
    expect(
      priorityQuery({ segment: "at_risk" }, { after: "cursor-3", limit: 50 }),
    ).toEqual({ segment: "at_risk", after: "cursor-3", limit: 50 });
  });

  it("drops a segment the endpoint does not know", () => {
    expect(
      priorityQuery({ segment: "unicorns" }, { after: "", limit: 20 }).segment,
    ).toBeUndefined();
  });
});
