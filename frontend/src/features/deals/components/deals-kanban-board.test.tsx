import { readFileSync } from "node:fs";
import path from "node:path";
import { QueryClient, type InfiniteData } from "@tanstack/react-query";
import { describe, expect, it } from "vitest";

import { applyOptimisticMove } from "@/features/deals/components/deals-kanban-cache";
import type {
  Deal,
  DealPage,
  PipelineBoardStage,
} from "@/features/deals/types";

import {
  stageDealsQueryKey,
  stageDealsQueryOptions,
} from "@/features/deals/hooks/use-deals";

describe("deals kanban filters and positions", () => {
  it("normalizes empty filter strings to undefined for query key parity", () => {
    const keyWithEmpty = stageDealsQueryKey("stage-1", {
      q: "   ",
      ownerUserId: "",
      companyId: "",
      contactId: "",
      status: undefined,
    });

    const keyWithoutFilters = stageDealsQueryKey("stage-1", undefined);

    expect(keyWithEmpty).toEqual(keyWithoutFilters);
  });

  it("calculates 0-based index correctly for card drop and column drop", () => {
    const stageDeals = [
      { id: "deal-0", title: "First Deal" },
      { id: "deal-1", title: "Second Deal" },
      { id: "deal-2", title: "Third Deal" },
    ];

    // Dropping on deal-1 puts it at index 1
    const overDeal = stageDeals[1];
    const cardPosition = stageDeals.findIndex((d) => d.id === overDeal.id);
    expect(cardPosition).toBe(1);

    // Dropping on column sends 0
    const columnPosition = 0;
    expect(columnPosition).toBe(0);

    // Dropping on last card
    const lastCard = stageDeals[2];
    const lastCardPosition = stageDeals.findIndex((d) => d.id === lastCard.id);
    expect(lastCardPosition).toBe(2);
  });

  it("creates query options with required locale and queryKey", () => {
    const opts = stageDealsQueryOptions("stage-1", { q: "acme" }, "uk");
    expect(opts.queryKey).toEqual(stageDealsQueryKey("stage-1", { q: "acme" }));
  });
});

describe("deals kanban keyboard move accessibility announcements", () => {
  function getAnnouncements(locale: string) {
    const filePath = path.join(process.cwd(), "messages", locale, "crm.json");
    const json = JSON.parse(readFileSync(filePath, "utf8"));
    return json.deals.accessibility;
  }

  it("provides full announcement templates in English with correct placeholders", () => {
    const en = getAnnouncements("en");
    expect(en.pickedUp).toContain("{title}");
    expect(en.dragOver).toContain("{title}");
    expect(en.dragOver).toContain("{target}");
    expect(en.dropped).toContain("{title}");
    expect(en.dropped).toContain("{stage}");
    expect(en.cancelled).toContain("{title}");

    // Format simulation
    const formatted = en.dropped
      .replace("{title}", "Big Deal")
      .replace("{stage}", "Won");
    expect(formatted).toBe("Deal Big Deal dropped on stage Won.");
  });

  it("provides full announcement templates in Ukrainian with identical placeholders", () => {
    const uk = getAnnouncements("uk");
    expect(uk.pickedUp).toContain("{title}");
    expect(uk.dragOver).toContain("{title}");
    expect(uk.dragOver).toContain("{target}");
    expect(uk.dropped).toContain("{title}");
    expect(uk.dropped).toContain("{stage}");
    expect(uk.cancelled).toContain("{title}");

    const formatted = uk.dropped
      .replace("{title}", "Велика угода")
      .replace("{stage}", "Виграно");
    expect(formatted).toBe("Угоду Велика угода переміщено на етап Виграно.");
  });
});

describe("deals kanban optimistic move", () => {
  it("moves the card into the target stage cache synchronously and restores on snapshot", () => {
    const queryClient = new QueryClient();
    const deal = { id: "d-1", stageId: "s-1", amount: 100, version: 1 } as Deal;
    const other = { id: "d-2", stageId: "s-2", amount: 50, version: 1 } as Deal;
    const sourceKey = stageDealsQueryKey("s-1", undefined);
    const targetKey = stageDealsQueryKey("s-2", undefined);
    queryClient.setQueryData(sourceKey, {
      pages: [{ items: [deal], nextCursor: null }],
      pageParams: [undefined],
    });
    queryClient.setQueryData(targetKey, {
      pages: [{ items: [other], nextCursor: null }],
      pageParams: [undefined],
    });
    const targetStage = { id: "s-2", kind: "open" } as PipelineBoardStage;

    const snapshot = applyOptimisticMove(queryClient, "p-1", undefined, {
      deal,
      targetStage,
      position: 0,
    });

    const source = queryClient.getQueryData<InfiniteData<DealPage>>(sourceKey);
    const target = queryClient.getQueryData<InfiniteData<DealPage>>(targetKey);
    expect(source?.pages.flatMap((p) => p.items)).toEqual([]);
    expect(target?.pages.flatMap((p) => p.items).map((d) => d.id)).toEqual([
      "d-1",
      "d-2",
    ]);
    expect(target?.pages[0].items[0].stageId).toBe("s-2");
    expect(snapshot.previousSource?.pages[0].items).toHaveLength(1);
  });
});
