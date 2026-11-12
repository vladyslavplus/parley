import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

import { AutosaveQueue } from "@/features/campaigns/lib/autosave-queue";
import {
  initialDraft,
  isTouched,
  type CampaignDraft,
} from "@/features/campaigns/lib/campaign-draft";

const DEBOUNCE = 100;

function draftNamed(name: string): CampaignDraft {
  return { ...initialDraft(), name, templateId: "t-1" };
}

function queueWith(
  save: AutosaveQueue["bindSave"] extends (s: infer S) => void ? S : never,
  isConflict = () => false,
) {
  return new AutosaveQueue({
    debounceMs: DEBOUNCE,
    isConflict,
    onState: () => undefined,
    save,
  });
}

describe("unsaved edits", () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it("counts an edit as unsaved from the keystroke until the server has it", async () => {
    const queue = queueWith(async () => ({ id: "c-1", version: 1 }));
    expect(queue.hasUnsaved).toBe(false);

    queue.schedule(draftNamed("A"));
    expect(queue.hasUnsaved).toBe(true); // inside the debounce window

    await vi.advanceTimersByTimeAsync(DEBOUNCE);
    expect(queue.hasUnsaved).toBe(false);
  });

  it("is unsaved while a save is in flight", async () => {
    let release: () => void = () => undefined;
    const queue = queueWith(
      () =>
        new Promise((resolve) => {
          release = () => resolve({ id: "c-1", version: 1 });
        }),
    );
    queue.schedule(draftNamed("A"));
    await vi.advanceTimersByTimeAsync(DEBOUNCE);
    expect(queue.hasUnsaved).toBe(true);
    release();
    await queue.flush();
    expect(queue.hasUnsaved).toBe(false);
  });

  it("reports failure from flush after a failed save, so nothing is frozen on stale data", async () => {
    const queue = queueWith(async () => {
      throw new Error("offline");
    });
    queue.schedule(draftNamed("A"));
    expect(await queue.flush()).toBe(false);
    expect(queue.hasUnsaved).toBe(true);
  });

  it("reports failure from flush while paused on a conflict", async () => {
    const conflict = new Error("409");
    const queue = queueWith(
      async () => {
        throw conflict;
      },
      () => true,
    );
    queue.schedule(draftNamed("A"));
    expect(await queue.flush()).toBe(false);
    queue.schedule(draftNamed("AB"));
    expect(await queue.flush()).toBe(false);
  });

  it("reports success once a retry stores the draft", async () => {
    let fail = true;
    const queue = queueWith(async () => {
      if (fail) throw new Error("offline");
      return { id: "c-1", version: 1 };
    });
    queue.schedule(draftNamed("A"));
    expect(await queue.flush()).toBe(false);
    fail = false;
    expect(await queue.flush()).toBe(true);
    expect(queue.hasUnsaved).toBe(false);
  });

  it("treats anything typed into a blank draft as touched", () => {
    expect(isTouched(initialDraft())).toBe(false);
    expect(isTouched({ ...initialDraft(), name: "x" })).toBe(true);
  });
});
