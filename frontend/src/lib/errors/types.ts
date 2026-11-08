export type ApiErrorKind =
  | "validation"
  | "unauthorized"
  | "forbidden"
  | "not_found"
  | "conflict"
  | "rate_limited"
  | "server"
  | "network"
  | "unknown";

export interface FieldErrorItem {
  code: string;
  message: string;
  params: Record<string, string>;
}

export type FieldErrors = Record<string, FieldErrorItem[]>;

export interface ApiErrorInit {
  kind: ApiErrorKind;
  status: number;
  code: string | null;
  detail: string | null;
  traceId: string | null;
  fieldErrors: FieldErrors;
}
