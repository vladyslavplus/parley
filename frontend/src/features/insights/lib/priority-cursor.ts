import { ERROR_CODES } from "@/constants/error-codes";

// Priority cursors are rank-based and go stale when the list is rebuilt. The server
// answers a stale cursor with a validation error; the list then restarts from the
// first page instead of showing that error.
export function isStaleCursorError(
  cursor: string,
  errorCode: string | null | undefined,
): boolean {
  return cursor !== "" && errorCode === ERROR_CODES.validationFailed;
}
