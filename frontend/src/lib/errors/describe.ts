import type { ApiError } from "./api-error";
import type { ApiErrorKind } from "./types";

export type FallbackTranslator = (kind: ApiErrorKind) => string;

// The server `detail` is already localized; client messages are only used
// when the server sent none (network failure, proxy errors, empty bodies).
export function describeError(
  error: ApiError,
  fallback: FallbackTranslator,
): string {
  return error.detail ?? fallback(error.kind);
}
