import { SEGMENT_CODES } from "@/features/insights/lib/codes";

export interface PriorityFilterValues {
  // RFM segment code from the URL; the empty string means every segment.
  segment: string;
}

export interface PriorityPaging {
  after: string;
  limit: number;
}

export type PriorityRequestQuery = Record<string, string | number | undefined>;

// Maps the URL state to the priority read. Unknown segment codes are dropped so
// a hand-edited URL never reaches the server as a bad request.
export function priorityQuery(
  values: PriorityFilterValues,
  paging: PriorityPaging,
): PriorityRequestQuery {
  const segment = (SEGMENT_CODES as readonly string[]).includes(values.segment)
    ? values.segment
    : undefined;
  return {
    segment,
    after: paging.after || undefined,
    limit: paging.limit,
  };
}
