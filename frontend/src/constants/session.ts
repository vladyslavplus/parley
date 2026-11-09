export const SESSION_COOKIES = {
  access: "parley_access",
  refresh: "parley_refresh",
} as const;

export const SESSION_COOKIE_PATH = "/";

// Used when a backend expiry timestamp cannot be parsed.
export const FALLBACK_ACCESS_MAX_AGE_SECONDS = 15 * 60;
export const FALLBACK_REFRESH_MAX_AGE_SECONDS = 30 * 24 * 60 * 60;

// How long a finished refresh result is shared with requests that still carry
// the previous refresh token (browser has not applied the new cookies yet).
export const REFRESH_SHARE_WINDOW_MS = 5_000;

export const HEADERS = {
  authorization: "Authorization",
  acceptLanguage: "Accept-Language",
  contentType: "Content-Type",
  accept: "Accept",
  cookie: "cookie",
  intlLocale: "X-NEXT-INTL-LOCALE",
  requestId: "x-request-id",
  forwardedFor: "X-Forwarded-For",
  retryAfter: "Retry-After",
  allow: "Allow",
  origin: "Origin",
  secFetchSite: "Sec-Fetch-Site",
  contentLength: "Content-Length",
  contentDisposition: "Content-Disposition",
  idempotencyKey: "Idempotency-Key",
} as const;

export const SEC_FETCH_SITE_SAME_ORIGIN = "same-origin";

// Methods that never change state; every other BFF handler runs the
// same-origin guard.
export const SAFE_METHODS: ReadonlySet<string> = new Set(["GET", "HEAD"]);

// Backend response headers the generic proxy hands back to the browser.
export const PROXY_RESPONSE_HEADERS = [
  HEADERS.contentType,
  HEADERS.retryAfter,
  HEADERS.contentDisposition,
  HEADERS.contentLength,
] as const;

export const MEDIA_TYPES = {
  json: "application/json",
  problem: "application/problem+json",
} as const;
