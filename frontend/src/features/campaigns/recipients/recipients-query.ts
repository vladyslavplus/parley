// Options the recipients endpoint accepts. Values outside these lists are
// dropped here, so a hand-edited URL never reaches the server as a bad request.
export const RECIPIENT_ARMS = ["treatment", "holdout"] as const;

export const RECIPIENT_SEND_STATUSES = [
  "claimed",
  "sent",
  "retryable_failed",
  "failed",
  "skipped_suppressed",
  "unknown",
] as const;
export type RecipientSendStatus = (typeof RECIPIENT_SEND_STATUSES)[number];

// Boolean filters travel as the text "true" or "false".
export const RECIPIENT_FLAGS = ["true", "false"] as const;

// Filter values as the URL holds them; the empty string means "no filter".
export interface RecipientFilterValues {
  q: string;
  arm: string;
  targeted: string;
  sendStatus: string;
  converted: string;
}

export interface RecipientPaging {
  after: string;
  limit: number;
}

export type RecipientsRequestQuery = Record<
  string,
  string | number | undefined
>;

function allowed<T extends string>(
  value: string,
  options: readonly T[],
): string | undefined {
  return (options as readonly string[]).includes(value) ? value : undefined;
}

// Maps the URL state to the query of the recipients read. Empty values are
// left out, so "all" is the absence of a parameter.
export function recipientsQuery(
  values: RecipientFilterValues,
  paging: RecipientPaging,
): RecipientsRequestQuery {
  return {
    q: values.q.trim() || undefined,
    arm: allowed(values.arm, RECIPIENT_ARMS),
    targeted: allowed(values.targeted, RECIPIENT_FLAGS),
    sendStatus: allowed(values.sendStatus, RECIPIENT_SEND_STATUSES),
    converted: allowed(values.converted, RECIPIENT_FLAGS),
    after: paging.after || undefined,
    limit: paging.limit,
  };
}
