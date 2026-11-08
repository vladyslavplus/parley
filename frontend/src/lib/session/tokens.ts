import {
  FALLBACK_ACCESS_MAX_AGE_SECONDS,
  FALLBACK_REFRESH_MAX_AGE_SECONDS,
  SESSION_COOKIES,
  SESSION_COOKIE_PATH,
} from "@/constants/session";
import type { Schemas } from "@/lib/api/schemas";

// Shape returned by the backend token endpoints. Only the BFF reads it; it is
// never serialised into a browser response.
export type TokenPair = Schemas["AuthTokens"];

export function isTokenPair(value: unknown): value is TokenPair {
  if (typeof value !== "object" || value === null) return false;
  const record = value as Record<string, unknown>;
  return (
    typeof record.accessToken === "string" &&
    typeof record.refreshToken === "string" &&
    record.accessToken.length > 0 &&
    record.refreshToken.length > 0
  );
}

export interface CookieWriter {
  set(options: {
    name: string;
    value: string;
    httpOnly: boolean;
    secure: boolean;
    sameSite: "lax";
    path: string;
    maxAge: number;
  }): unknown;
  delete(name: string): unknown;
}

export function maxAgeFromExpiry(
  expiresAt: string | undefined,
  fallbackSeconds: number,
  now: number = Date.now(),
): number {
  const parsed = expiresAt ? Date.parse(expiresAt) : Number.NaN;
  if (Number.isNaN(parsed)) return fallbackSeconds;
  return Math.max(0, Math.floor((parsed - now) / 1000));
}

function isSecureContext(): boolean {
  return process.env.NODE_ENV === "production";
}

export function writeSessionCookies(
  cookies: CookieWriter,
  tokens: TokenPair,
): void {
  const base = {
    httpOnly: true,
    secure: isSecureContext(),
    sameSite: "lax" as const,
    path: SESSION_COOKIE_PATH,
  };
  cookies.set({
    ...base,
    name: SESSION_COOKIES.access,
    value: tokens.accessToken,
    maxAge: maxAgeFromExpiry(
      tokens.accessTokenExpiresAt,
      FALLBACK_ACCESS_MAX_AGE_SECONDS,
    ),
  });
  cookies.set({
    ...base,
    name: SESSION_COOKIES.refresh,
    value: tokens.refreshToken,
    maxAge: maxAgeFromExpiry(
      tokens.refreshTokenExpiresAt,
      FALLBACK_REFRESH_MAX_AGE_SECONDS,
    ),
  });
}

export function clearSessionCookies(cookies: CookieWriter): void {
  cookies.delete(SESSION_COOKIES.access);
  cookies.delete(SESSION_COOKIES.refresh);
}
