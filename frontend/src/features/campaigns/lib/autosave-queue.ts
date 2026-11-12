import type { CampaignDraft } from "@/features/campaigns/lib/campaign-draft";

export type AutosaveState = "idle" | "saving" | "saved" | "error" | "conflict";

export interface SavedCampaign {
  id: string;
  version: number;
}

export interface SaveCursor {
  id: string | null;
  version: number | null;
}

interface Options {
  save: (draft: CampaignDraft, cursor: SaveCursor) => Promise<SavedCampaign>;
  isConflict: (error: unknown) => boolean;
  onState: (state: AutosaveState, error: unknown) => void;
  debounceMs: number;
}

function draftKey(draft: CampaignDraft): string {
  return JSON.stringify(draft);
}

// Serializes draft saves: at most one request is in flight, later edits are
// coalesced and sent with the version the previous save returned, so the
// client never conflicts with itself. A real conflict (409) pauses saving and
// keeps the unsaved draft untouched until the caller chooses what to do.
export class AutosaveQueue {
  private cursor: SaveCursor = { id: null, version: null };
  private pending: CampaignDraft | null = null;
  private timer: ReturnType<typeof setTimeout> | null = null;
  private running: Promise<void> | null = null;
  private paused = false;
  private savedKey: string | null = null;
  private failed = false;

  constructor(private readonly options: Options) {}

  // The owner swaps in the latest `save` closure after each render.
  bindSave(save: Options["save"]): void {
    this.options.save = save;
  }

  get id(): string | null {
    return this.cursor.id;
  }

  schedule(draft: CampaignDraft): void {
    // Nothing differs from what the server already holds: no request.
    if (!this.paused && !this.running && draftKey(draft) === this.savedKey) {
      this.pending = null;
      if (this.timer) clearTimeout(this.timer);
      this.timer = null;
      return;
    }
    this.pending = draft;
    if (this.paused) return;
    if (this.timer) clearTimeout(this.timer);
    this.timer = setTimeout(() => {
      this.timer = null;
      void this.drain();
    }, this.options.debounceMs);
  }

  // True while an edit exists that the server does not have yet: waiting in
  // the debounce, queued, in flight, or stuck after a failed save.
  get hasUnsaved(): boolean {
    return (
      this.pending !== null || this.timer !== null || this.running !== null
    );
  }

  // Sends the pending draft now. Resolves true only when everything the user
  // changed is stored; false after a failed save or while paused on a conflict.
  async flush(): Promise<boolean> {
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = null;
    }
    if (!this.paused) await this.drain();
    else if (this.running) await this.running;
    return !this.paused && !this.failed && this.pending === null;
  }

  // Takes the server row as the baseline, e.g. a campaign opened by URL or
  // reloaded after a conflict. Pending local edits are dropped.
  seed(cursor: SaveCursor, draft: CampaignDraft): void {
    if (this.timer) clearTimeout(this.timer);
    this.timer = null;
    this.pending = null;
    this.paused = false;
    this.failed = false;
    this.cursor = { ...cursor };
    this.savedKey = draftKey(draft);
  }

  resetTo(cursor: SaveCursor, draft: CampaignDraft): void {
    this.seed(cursor, draft);
    this.options.onState("saved", null);
  }

  // After a conflict: abandon the stale row and store the kept draft as a new
  // campaign so the user's input is never discarded.
  async restartAsNew(): Promise<void> {
    this.cursor = { id: null, version: null };
    this.savedKey = null;
    this.paused = false;
    this.failed = false;
    await this.flush();
  }

  dispose(): void {
    if (this.timer) clearTimeout(this.timer);
    this.timer = null;
    this.pending = null;
  }

  private drain(): Promise<void> {
    if (!this.running) {
      this.running = this.loop().finally(() => {
        this.running = null;
      });
    }
    return this.running;
  }

  private async loop(): Promise<void> {
    this.failed = false;
    while (this.pending && !this.paused) {
      const draft = this.pending;
      this.pending = null;
      this.options.onState("saving", null);
      try {
        const saved = await this.options.save(draft, { ...this.cursor });
        this.cursor = { id: saved.id, version: saved.version };
        this.savedKey = draftKey(draft);
      } catch (error) {
        // Keep whatever the user typed meanwhile; fall back to the draft that
        // just failed so nothing is lost.
        this.pending ??= draft;
        this.failed = true;
        if (this.options.isConflict(error)) {
          this.paused = true;
          this.options.onState("conflict", error);
        } else {
          this.options.onState("error", error);
        }
        return;
      }
    }
    if (!this.paused) this.options.onState("saved", null);
  }
}
