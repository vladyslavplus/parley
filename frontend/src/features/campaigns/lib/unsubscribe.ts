import { ERROR_CODES } from "@/constants/error-codes";
import { toApiError } from "@/lib/errors";

export type UnsubscribeFailure = "invalid" | "rate_limited" | "failed";

const INVALID_CODES: readonly string[] = [
  ERROR_CODES.unsubscribeTokenInvalid,
  ERROR_CODES.unsubscribeTokenExpired,
];

const BAD_REQUEST = 400;
const TOO_MANY_REQUESTS = 429;

// Which message the page shows for a failed unsubscribe. A bad, expired or
// missing token all read the same to the reader; nothing technical is shown.
export function unsubscribeFailure(error: unknown): UnsubscribeFailure {
  const apiError = toApiError(error);
  if (apiError.status === TOO_MANY_REQUESTS) return "rate_limited";
  if (
    apiError.status === BAD_REQUEST ||
    (apiError.code !== null && INVALID_CODES.includes(apiError.code))
  ) {
    return "invalid";
  }
  return "failed";
}
