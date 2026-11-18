import {
  SEGMENT_CODES,
  UPLIFT_SEGMENT_CODES,
} from "@/features/insights/lib/codes";

export interface PriorityFilterValues {
  // RFM segment code from the URL; the empty string means every segment.
  segment: string;
  // Uplift segment code from the URL; the empty string means every segment.
  upliftSegment: string;
}

export interface PriorityPaging {
  after: string;
  limit: number;
}

export type PriorityRequestQuery = Record<string, string | number | undefined>;

// Maps the URL state to the priority read. The two segment dimensions combine;
// unknown codes are dropped so a hand-edited URL never reaches the server as a
// bad request.
export function priorityQuery(
  values: PriorityFilterValues,
  paging: PriorityPaging,
): PriorityRequestQuery {
  const segment = (SEGMENT_CODES as readonly string[]).includes(values.segment)
    ? values.segment
    : undefined;
  const upliftSegment = (UPLIFT_SEGMENT_CODES as readonly string[]).includes(
    values.upliftSegment,
  )
    ? values.upliftSegment
    : undefined;
  return {
    segment,
    upliftSegment,
    after: paging.after || undefined,
    limit: paging.limit,
  };
}
