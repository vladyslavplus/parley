import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { AutosaveQueue } from "@/features/campaigns/lib/autosave-queue";
import {
  draftFromCampaign,
  draftToRequest,
} from "@/features/campaigns/lib/campaign-draft";
import type { Campaign } from "@/features/campaigns/types";

const DEBOUNCE = 100;

const campaign: Campaign = {
  id: "c-1",
  name: "Spring",
  templateId: "t-1",
  segmentId: "s-1",
  status: "draft",
  holdoutShareBps: 1500,
  holdoutSeed: "seed",
  targetingMode: "top_uplift",
  targetingShareBps: 2500,
  audienceFrozenAt: null,
  targetingModelName: null,
  targetingModelVersion: null,
  targetingProvenance: null,
  version: "42",
  source: "user",
};

describe("resuming a campaign", () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it("turns the server row into a draft that sends back the same request", () => {
    const draft = draftFromCampaign(campaign);
    expect(draft).toEqual({
      name: "Spring",
      templateId: "t-1",
      segmentId: "s-1",
      holdoutShareBps: 1500,
      targetingMode: "top_uplift",
      targetingShareBps: 2500,
    });
    expect(draftToRequest(draft, 42)).toMatchObject({
      targetingShareBps: 2500,
      version: 42,
    });
  });

  it("falls back to the default share when the server has none", () => {
    const draft = draftFromCampaign({
      ...campaign,
      targetingMode: "all",
      targetingShareBps: null,
    });
    expect(draft.targetingMode).toBe("all");
    expect(draftToRequest(draft, 1).targetingShareBps).toBeNull();
  });

  it("sends nothing for the unchanged hydrated draft and chains the loaded version on the first edit", async () => {
    const calls: Array<{ name: string; version: number | null }> = [];
    const queue = new AutosaveQueue({
      debounceMs: DEBOUNCE,
      isConflict: () => false,
      onState: () => undefined,
      save: async (draft, cursor) => {
        calls.push({ name: draft.name, version: cursor.version });
        return { id: "c-1", version: (cursor.version ?? 0) + 1 };
      },
    });
    const draft = draftFromCampaign(campaign);
    queue.seed({ id: "c-1", version: 42 }, draft);

    queue.schedule(draft);
    await vi.advanceTimersByTimeAsync(DEBOUNCE * 2);
    expect(calls).toEqual([]);

    queue.schedule({ ...draft, name: "Spring 2" });
    await vi.advanceTimersByTimeAsync(DEBOUNCE);
    expect(calls).toEqual([{ name: "Spring 2", version: 42 }]);
  });

  it("after a conflict, loading the latest version drops the stale edits and resumes saving from the new version", async () => {
    const conflict = new Error("409");
    const calls: Array<{ name: string; version: number | null }> = [];
    let failNext = true;
    const states: string[] = [];
    const queue = new AutosaveQueue({
      debounceMs: DEBOUNCE,
      isConflict: (error) => error === conflict,
      onState: (state) => states.push(state),
      save: async (draft, cursor) => {
        if (failNext) throw conflict;
        calls.push({ name: draft.name, version: cursor.version });
        return { id: "c-1", version: (cursor.version ?? 0) + 1 };
      },
    });
    const draft = draftFromCampaign(campaign);
    queue.seed({ id: "c-1", version: 42 }, draft);

    queue.schedule({ ...draft, name: "Local edit" });
    await queue.flush();
    expect(states.at(-1)).toBe("conflict");

    const latest = { ...draft, name: "Edited elsewhere" };
    queue.resetTo({ id: "c-1", version: 50 }, latest);
    expect(states.at(-1)).toBe("saved");

    // The effect that follows a reload schedules the loaded draft: no request.
    failNext = false;
    queue.schedule(latest);
    await vi.advanceTimersByTimeAsync(DEBOUNCE * 2);
    expect(calls).toEqual([]);

    queue.schedule({ ...latest, name: "Edited elsewhere, again" });
    await vi.advanceTimersByTimeAsync(DEBOUNCE);
    expect(calls).toEqual([{ name: "Edited elsewhere, again", version: 50 }]);
  });
});
