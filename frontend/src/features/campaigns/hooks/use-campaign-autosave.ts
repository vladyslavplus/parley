"use client";

import { useEffect, useState } from "react";

import { ERROR_CODES } from "@/constants/error-codes";
import { LIMITS } from "@/constants/limits";
import { useCampaignApi } from "@/features/campaigns/hooks/use-campaign-api";
import {
  AutosaveQueue,
  type AutosaveState,
  type SaveCursor,
  type SavedCampaign,
} from "@/features/campaigns/lib/autosave-queue";
import {
  draftFromCampaign,
  draftToRequest,
  isSaveable,
  isTouched,
  type CampaignDraft,
} from "@/features/campaigns/lib/campaign-draft";
import { toNumber, type Campaign } from "@/features/campaigns/types";
import { toApiError } from "@/lib/errors";

function isVersionConflict(error: unknown): boolean {
  const apiError = toApiError(error);
  return (
    apiError.status === 409 &&
    (apiError.code === ERROR_CODES.campaignVersionConflict ||
      apiError.code === ERROR_CODES.concurrencyConflict)
  );
}

function seeded(queue: AutosaveQueue, initial?: Campaign): AutosaveQueue {
  if (initial) {
    queue.seed(
      { id: initial.id, version: toNumber(initial.version) },
      draftFromCampaign(initial),
    );
  }
  return queue;
}

// Autosaves the draft through a serialized queue. The draft is sent once it is
// saveable (name and template); edits are debounced and chained on the version
// the server returned last.
export function useCampaignAutosave(draft: CampaignDraft, initial?: Campaign) {
  const api = useCampaignApi();
  const [state, setState] = useState<AutosaveState>("idle");
  const [error, setError] = useState<unknown>(null);
  const [campaignId, setCampaignId] = useState<string | null>(
    initial?.id ?? null,
  );

  async function save(
    next: CampaignDraft,
    cursor: SaveCursor,
  ): Promise<SavedCampaign> {
    const body = draftToRequest(next, cursor.version);
    const result = cursor.id
      ? await api.updateCampaign(cursor.id, body)
      : await api.createCampaign(body);
    setCampaignId(result.id);
    return { id: result.id, version: toNumber(result.version) };
  }

  // The queue outlives renders; it always calls the latest `save`.
  const [queue] = useState(() =>
    seeded(
      new AutosaveQueue({
        debounceMs: LIMITS.campaignAutosaveDebounceMs,
        isConflict: isVersionConflict,
        save: () => Promise.reject(new Error("autosave is not bound")),
        onState: (nextState, nextError) => {
          setState(nextState);
          setError(nextError);
        },
      }),
      initial,
    ),
  );
  useEffect(() => {
    queue.bindSave(save);
  });

  useEffect(() => {
    if (isSaveable(draft)) queue.schedule(draft);
  }, [draft, queue]);

  // Leaving the builder sends whatever is still pending.
  useEffect(() => {
    return () => {
      void queue.flush();
    };
  }, [queue]);

  // Edits typed before the draft can be stored (no name or template yet) never
  // reach the queue, so they count as unsaved too.
  const blocked = !isSaveable(draft) && isTouched(draft);
  const atRisk = state === "error" || state === "conflict" || blocked;

  // A tab that is hidden or closing sends what it can right away and warns
  // while an edit is still only in this browser.
  useEffect(() => {
    function sendNow() {
      if (document.visibilityState === "hidden") void queue.flush();
    }
    function warn(event: BeforeUnloadEvent) {
      void queue.flush();
      if (queue.hasUnsaved || atRisk) event.preventDefault();
    }
    document.addEventListener("visibilitychange", sendNow);
    window.addEventListener("pagehide", sendNow);
    window.addEventListener("beforeunload", warn);
    return () => {
      document.removeEventListener("visibilitychange", sendNow);
      window.removeEventListener("pagehide", sendNow);
      window.removeEventListener("beforeunload", warn);
    };
  }, [queue, atRisk]);

  return {
    state,
    error,
    campaignId,
    flush: () => queue.flush(),
    retry: () => {
      if (isSaveable(draft)) queue.schedule(draft);
      return queue.flush();
    },
    saveAsNew: () => queue.restartAsNew(),
    // Takes a freshly loaded server row as the new baseline (after a conflict).
    adopt: (latest: Campaign) => {
      queue.resetTo(
        { id: latest.id, version: toNumber(latest.version) },
        draftFromCampaign(latest),
      );
      setCampaignId(latest.id);
    },
  };
}
