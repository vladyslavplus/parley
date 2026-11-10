"use client";

import { useSegmentCount } from "@/features/campaigns/components/segment-count";
import { useSegment } from "@/features/campaigns/hooks/use-library";
import type { SegmentDefinition } from "@/features/campaigns/types";

// No filter at all: every contact with an address.
export const EVERYONE_DEFINITION: SegmentDefinition = {
  ownerUserIds: [],
  countryCodes: [],
  tagIds: [],
  rfmSegments: [],
  daysSinceLastOrder: null,
};

// Size of the audience a draft points at (a saved segment, or everyone).
export function useAudienceCount(segmentId: string | null) {
  const segment = useSegment(segmentId);
  const definition = segmentId
    ? (segment.data?.definition ?? null)
    : EVERYONE_DEFINITION;
  const counted = useSegmentCount(definition ?? EVERYONE_DEFINITION);
  return {
    count: definition ? counted.count : null,
    error: segment.error ?? counted.error,
  };
}
