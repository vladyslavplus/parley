import { describe, expect, it } from "vitest";

import { priorityQuery } from "@/features/insights/lib/priority-query";

describe("priority query", () => {
  it("sends only the paging when no segment is chosen", () => {
    expect(
      priorityQuery(
        { segment: "", upliftSegment: "" },
        { after: "", limit: 20 },
      ),
    ).toEqual({
      segment: undefined,
      upliftSegment: undefined,
      after: undefined,
      limit: 20,
    });
  });

  it("passes a known segment and the cursor through", () => {
    expect(
      priorityQuery(
        { segment: "at_risk", upliftSegment: "" },
        { after: "cursor-3", limit: 50 },
      ),
    ).toEqual({
      segment: "at_risk",
      upliftSegment: undefined,
      after: "cursor-3",
      limit: 50,
    });
  });

  it("combines both segment dimensions", () => {
    expect(
      priorityQuery(
        { segment: "champions", upliftSegment: "persuadable" },
        { after: "", limit: 20 },
      ),
    ).toEqual({
      segment: "champions",
      upliftSegment: "persuadable",
      after: undefined,
      limit: 20,
    });
  });

  it("drops a segment the endpoint does not know", () => {
    expect(
      priorityQuery(
        { segment: "unicorns", upliftSegment: "unicorns" },
        { after: "", limit: 20 },
      ),
    ).toEqual({
      segment: undefined,
      upliftSegment: undefined,
      after: undefined,
      limit: 20,
    });
  });
});
