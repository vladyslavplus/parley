import type {
  ApiErrorInit,
  ApiErrorKind,
  FieldErrorItem,
  FieldErrors,
} from "./types";

export const NETWORK_ERROR_STATUS = 0;

export class ApiError extends Error implements ApiErrorInit {
  readonly kind: ApiErrorKind;
  readonly status: number;
  readonly code: string | null;
  readonly detail: string | null;
  readonly traceId: string | null;
  readonly fieldErrors: FieldErrors;

  constructor(init: ApiErrorInit) {
    super(init.detail ?? init.code ?? init.kind);
    this.name = "ApiError";
    this.kind = init.kind;
    this.status = init.status;
    this.code = init.code;
    this.detail = init.detail;
    this.traceId = init.traceId;
    this.fieldErrors = init.fieldErrors;
  }
}

export function kindFromStatus(status: number): ApiErrorKind {
  if (status === NETWORK_ERROR_STATUS) return "network";
  if (status === 400 || status === 422) return "validation";
  if (status === 401) return "unauthorized";
  if (status === 403) return "forbidden";
  if (status === 404) return "not_found";
  if (status === 409) return "conflict";
  if (status === 429) return "rate_limited";
  if (status >= 500) return "server";
  return "unknown";
}

function asString(value: unknown): string | null {
  return typeof value === "string" && value.length > 0 ? value : null;
}

function parseFieldErrors(value: unknown): FieldErrors {
  const result: FieldErrors = {};
  if (typeof value !== "object" || value === null) return result;
  for (const [field, items] of Object.entries(value)) {
    if (!Array.isArray(items)) continue;
    const parsed: FieldErrorItem[] = [];
    for (const item of items) {
      if (typeof item !== "object" || item === null) continue;
      const record = item as Record<string, unknown>;
      const message = asString(record.message);
      if (!message) continue;
      const params: Record<string, string> = {};
      if (typeof record.params === "object" && record.params !== null) {
        for (const [key, param] of Object.entries(record.params)) {
          params[key] = String(param);
        }
      }
      parsed.push({ code: asString(record.code) ?? "", message, params });
    }
    if (parsed.length > 0) result[field] = parsed;
  }
  return result;
}

export function apiErrorFromProblem(status: number, body: unknown): ApiError {
  const record =
    typeof body === "object" && body !== null
      ? (body as Record<string, unknown>)
      : {};
  return new ApiError({
    kind: kindFromStatus(status),
    status,
    code: asString(record.code),
    detail: asString(record.detail),
    traceId: asString(record.traceId),
    fieldErrors: parseFieldErrors(record.errors),
  });
}

export async function apiErrorFromResponse(
  response: Response,
): Promise<ApiError> {
  let body: unknown = null;
  try {
    body = await response.json();
  } catch {
    body = null;
  }
  return apiErrorFromProblem(response.status, body);
}

export function networkError(): ApiError {
  return new ApiError({
    kind: "network",
    status: NETWORK_ERROR_STATUS,
    code: null,
    detail: null,
    traceId: null,
    fieldErrors: {},
  });
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;
  return networkError();
}
