import { NextResponse } from "next/server";

import { HEADERS, MEDIA_TYPES, SESSION_COOKIES } from "@/constants/session";
import { ERROR_CODES } from "@/constants/error-codes";
import { ROUTE_PARAMS } from "@/constants/routes";
import { problemResponse } from "@/lib/api/problem";
import { rejectUnsafeRequest } from "@/lib/bff/request-guard";
import {
  backendFetch,
  contextOf,
  postBackend,
  type PostPath,
} from "@/lib/bff/server";
import { refreshSession } from "@/lib/session/refresh";
import {
  clearSessionCookies,
  isTokenPair,
  writeSessionCookies,
} from "@/lib/session/tokens";

type RouteRequest = Request & {
  cookies?: { get(name: string): { value: string } | undefined };
};

export function readCookie(request: Request, name: string): string | undefined {
  const fromJar = (request as RouteRequest).cookies?.get(name)?.value;
  if (fromJar) return fromJar;
  const header = request.headers.get(HEADERS.cookie) ?? "";
  for (const part of header.split(";")) {
    const [key, ...rest] = part.trim().split("=");
    if (key === name) return decodeURIComponent(rest.join("="));
  }
  return undefined;
}

// Error bodies of the backend are ProblemDetails with localized text and no
// secrets; they are passed through unchanged.
export async function passThroughProblem(
  response: Response,
): Promise<NextResponse> {
  const text = await response.text();
  const headers: Record<string, string> = {
    [HEADERS.contentType]: MEDIA_TYPES.problem,
  };
  const retryAfter = response.headers.get(HEADERS.retryAfter);
  if (retryAfter) headers[HEADERS.retryAfter] = retryAfter;
  return new NextResponse(text || null, { status: response.status, headers });
}

export function backendUnavailable(): NextResponse {
  return problemResponse(502, ERROR_CODES.unexpected);
}

async function readTokens(response: Response) {
  const body: unknown = await response.json().catch(() => null);
  return isTokenPair(body) ? body : null;
}

// Forwards a body to a public auth endpoint and answers with an empty body.
export function createPublicHandler(path: PostPath) {
  return async function POST(request: Request): Promise<NextResponse> {
    const rejected = rejectUnsafeRequest(request);
    if (rejected) return rejected;
    let response: Response;
    try {
      response = await postBackend(path, {
        body: await request.text(),
        ...contextOf(request),
      });
    } catch {
      return backendUnavailable();
    }
    if (!response.ok) return passThroughProblem(response);
    return new NextResponse(null, {
      status: response.status === 202 ? 202 : 204,
    });
  };
}

// Anonymous unsubscribe: the signed token travels in the query string and is
// the only credential. Always a POST; the backend answers 204 and is idempotent.
export async function unsubscribeHandler(
  request: Request,
): Promise<NextResponse> {
  const rejected = rejectUnsafeRequest(request);
  if (rejected) return rejected;
  const token = new URL(request.url).searchParams.get(ROUTE_PARAMS.token);
  let response: Response;
  try {
    response = await postBackend("/unsubscribe", {
      search: `?${new URLSearchParams({ [ROUTE_PARAMS.token]: token ?? "" })}`,
      ...contextOf(request),
    });
  } catch {
    return backendUnavailable();
  }
  if (!response.ok) return passThroughProblem(response);
  return new NextResponse(null, { status: 204 });
}

// Forwards credentials; on success the tokens go to httpOnly cookies and the
// response body stays empty.
export function createSessionStartHandler(path: PostPath) {
  return async function POST(request: Request): Promise<NextResponse> {
    const rejected = rejectUnsafeRequest(request);
    if (rejected) return rejected;
    let response: Response;
    try {
      response = await postBackend(path, {
        body: await request.text(),
        ...contextOf(request),
      });
    } catch {
      return backendUnavailable();
    }
    if (!response.ok) return passThroughProblem(response);
    const tokens = await readTokens(response);
    if (!tokens) return backendUnavailable();
    const result = new NextResponse(null, { status: 204 });
    writeSessionCookies(result.cookies, tokens);
    return result;
  };
}

export async function logoutHandler(request: Request): Promise<NextResponse> {
  const rejected = rejectUnsafeRequest(request);
  if (rejected) return rejected;
  const refreshToken = readCookie(request, SESSION_COOKIES.refresh);
  if (refreshToken) {
    try {
      await postBackend("/auth/logout", {
        body: JSON.stringify({ refreshToken }),
        ...contextOf(request),
      });
    } catch {
      // The local session is cleared either way.
    }
  }
  const result = new NextResponse(null, { status: 204 });
  clearSessionCookies(result.cookies);
  return result;
}

export async function refreshHandler(request: Request): Promise<NextResponse> {
  const unsafe = rejectUnsafeRequest(request);
  if (unsafe) return unsafe;
  const refreshToken = readCookie(request, SESSION_COOKIES.refresh);
  if (!refreshToken) return new NextResponse(null, { status: 401 });
  const outcome = await refreshSession(refreshToken, contextOf(request));
  if (outcome.kind === "unavailable") return backendUnavailable();
  if (outcome.kind === "rejected") {
    const result = new NextResponse(null, { status: 401 });
    clearSessionCookies(result.cookies);
    return result;
  }
  const result = new NextResponse(null, { status: 204 });
  writeSessionCookies(result.cookies, outcome.tokens);
  return result;
}

export async function switchOrganizationHandler(
  request: Request,
): Promise<NextResponse> {
  const unsafe = rejectUnsafeRequest(request);
  if (unsafe) return unsafe;
  let accessToken = readCookie(request, SESSION_COOKIES.access);
  let refreshToken = readCookie(request, SESSION_COOKIES.refresh);
  const body: unknown = await request.json().catch(() => null);
  const organizationId =
    typeof body === "object" && body !== null
      ? (body as Record<string, unknown>).organizationId
      : null;
  if (!refreshToken) return new NextResponse(null, { status: 401 });
  if (typeof organizationId !== "string")
    return new NextResponse(null, { status: 400 });

  // The access cookie may have expired while the page stayed open; renew the
  // pair first so the switch call carries a valid access and refresh token.
  if (!accessToken) {
    const outcome = await refreshSession(refreshToken, contextOf(request));
    if (outcome.kind === "unavailable") return backendUnavailable();
    if (outcome.kind === "rejected") {
      const rejected = new NextResponse(null, { status: 401 });
      clearSessionCookies(rejected.cookies);
      return rejected;
    }
    accessToken = outcome.tokens.accessToken;
    refreshToken = outcome.tokens.refreshToken;
  }

  let response: Response;
  try {
    response = await backendFetch({
      method: "POST",
      path: `/organizations/${encodeURIComponent(organizationId)}/switch`,
      accessToken,
      ...contextOf(request),
      body: JSON.stringify({ refreshToken }),
    });
  } catch {
    return backendUnavailable();
  }
  if (response.status === 401) {
    const rejected = new NextResponse(null, { status: 401 });
    clearSessionCookies(rejected.cookies);
    return rejected;
  }
  if (!response.ok) return passThroughProblem(response);
  const tokens = await readTokens(response);
  if (!tokens) return backendUnavailable();
  const result = new NextResponse(null, { status: 204 });
  writeSessionCookies(result.cookies, tokens);
  return result;
}
