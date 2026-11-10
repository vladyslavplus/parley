import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import {
  AutosaveQueue,
  type AutosaveState,
} from "@/features/campaigns/lib/autosave-queue";
import {
  initialDraft,
  type CampaignDraft,
} from "@/features/campaigns/lib/campaign-draft";

const DEBOUNCE = 100;

function draftNamed(name: string): CampaignDraft {
  return { ...initialDraft(), name, templateId: "t-1" };
}

describe("AutosaveQueue", () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it("coalesces rapid edits and chains saves with the version the server returned", async () => {
    const calls: Array<{ name: string; version: number | null }> = [];
    let release: (() => void) | null = null;
    const states: AutosaveState[] = [];
    const queue = new AutosaveQueue({
      debounceMs: DEBOUNCE,
      isConflict: () => false,
      onState: (state) => states.push(state),
      save: async (draft, cursor) => {
        calls.push({ name: draft.name, version: cursor.version });
        if (calls.length === 1) {
          await new Promise<void>((resolve) => (release = resolve));
        }
        return { id: "c-1", version: (cursor.version ?? 0) + 1 };
      },
    });

    queue.schedule(draftNamed("A"));
    queue.schedule(draftNamed("AB"));
    await vi.advanceTimersByTimeAsync(DEBOUNCE);
    expect(calls).toEqual([{ name: "AB", version: null }]);

    // Edit while the first request is still in flight.
    queue.schedule(draftNamed("ABC"));
    await vi.advanceTimersByTimeAsync(DEBOUNCE);
    expect(calls).toHaveLength(1);

    (release as (() => void) | null)?.();
    await queue.flush();
    expect(calls).toEqual([
      { name: "AB", version: null },
      { name: "ABC", version: 1 },
    ]);
    expect(states.at(-1)).toBe("saved");
  });

  it("pauses on a conflict, keeps the draft, and restarts it as a new campaign", async () => {
    const conflict = new Error("409");
    const saved: Array<{ name: string; id: string | null }> = [];
    let failNext = false;
    const states: AutosaveState[] = [];
    const queue = new AutosaveQueue({
      debounceMs: DEBOUNCE,
      isConflict: (error) => error === conflict,
      onState: (state) => states.push(state),
      save: async (draft, cursor) => {
        if (failNext) throw conflict;
        saved.push({ name: draft.name, id: cursor.id });
        return { id: cursor.id ?? "c-1", version: 1 };
      },
    });

    queue.schedule(draftNamed("first"));
    await queue.flush();
    expect(queue.id).toBe("c-1");

    failNext = true;
    queue.schedule(draftNamed("second"));
    await queue.flush();
    expect(states.at(-1)).toBe("conflict");

    // Further edits are kept but not sent while paused.
    queue.schedule(draftNamed("third"));
    await vi.advanceTimersByTimeAsync(DEBOUNCE * 3);
    expect(saved).toHaveLength(1);

    failNext = false;
    await queue.restartAsNew();
    expect(saved.at(-1)).toEqual({ name: "third", id: null });
    expect(states.at(-1)).toBe("saved");
  });
});
