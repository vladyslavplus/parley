import type { NextResponse } from "next/server";

import { ERROR_CODES } from "@/constants/error-codes";
import {
  HEADERS,
  MEDIA_TYPES,
  SAFE_METHODS,
  SEC_FETCH_SITE_SAME_ORIGIN,
} from "@/constants/session";

import { problemResponse } from "@/lib/api/problem";

const FORBIDDEN_STATUS = 403;

function isSameOrigin(request: Request): boolean {
  const fetchSite = request.headers.get(HEADERS.secFetchSite);
  if (fetchSite !== null) return fetchSite === SEC_FETCH_SITE_SAME_ORIGIN;
  const origin = request.headers.get(HEADERS.origin);
  return origin !== null && origin === new URL(request.url).origin;
}

function hasBody(request: Request): boolean {
  return (
    request.body !== null && request.headers.get(HEADERS.contentLength) !== "0"
  );
}

function isJson(request: Request): boolean {
  const type = request.headers.get(HEADERS.contentType) ?? "";
  return type.split(";")[0]?.trim().toLowerCase() === MEDIA_TYPES.json;
}

// Shared guard for every state-changing BFF handler: the call must come from
// this site (blocks cross-site form posts such as login CSRF) and a body must
// be JSON (a cross-site form cannot send that content type).
export function rejectUnsafeRequest(request: Request): NextResponse | null {
  if (SAFE_METHODS.has(request.method.toUpperCase())) return null;
  if (!isSameOrigin(request) || (hasBody(request) && !isJson(request))) {
    return problemResponse(FORBIDDEN_STATUS, ERROR_CODES.requestRejected);
  }
  return null;
}
