import { NextResponse, type NextRequest } from "next/server";

import { ERROR_CODES } from "@/constants/error-codes";
import type { ProxyMethod } from "@/constants/proxy-routes";
import {
  HEADERS,
  PROXY_RESPONSE_HEADERS,
  SESSION_COOKIES,
} from "@/constants/session";
import { backendUnavailable } from "@/lib/bff/handlers";
import { matchProxyRoute } from "@/lib/bff/proxy-allow-list";
import { problemResponse } from "@/lib/api/problem";
import { rejectUnsafeRequest } from "@/lib/bff/request-guard";
import { backendFetch, contextOf } from "@/lib/bff/server";
import { refreshSession } from "@/lib/session/refresh";
import { clearSessionCookies, writeSessionCookies } from "@/lib/session/tokens";

type Context = { params: Promise<{ path: string[] }> };

const BODY_METHODS = new Set(["POST", "PUT", "PATCH"]);
const NOT_FOUND = 404;
const METHOD_NOT_ALLOWED = 405;
const UNAUTHORIZED = 401;

function unauthorized(): NextResponse {
  const response = problemResponse(UNAUTHORIZED, ERROR_CODES.authTokenInvalid);
  clearSessionCookies(response.cookies);
  return response;
}

async function handle(request: NextRequest, context: Context) {
  const { path } = await context.params;
  const match = matchProxyRoute(request.method, path);
  if (match.kind === "not-found") {
    return problemResponse(NOT_FOUND, ERROR_CODES.requestNotFound);
  }
  if (match.kind === "method-not-allowed") {
    return problemResponse(METHOD_NOT_ALLOWED, ERROR_CODES.requestNotFound, {
      [HEADERS.allow]: match.allowed.join(", "),
    });
  }
  const rejected = rejectUnsafeRequest(request);
  if (rejected) return rejected;

  const method = request.method as ProxyMethod;
  const body = BODY_METHODS.has(method) ? await request.text() : undefined;
  const backendPath = `/${path.map(encodeURIComponent).join("/")}`;
  const backendContext = contextOf(request);
  let accessToken = request.cookies.get(SESSION_COOKIES.access)?.value;
  const refreshToken = request.cookies.get(SESSION_COOKIES.refresh)?.value;

  const call = (token: string | undefined) =>
    backendFetch({
      ...backendContext,
      method,
      path: backendPath,
      search: request.nextUrl.search,
      body,
      accessToken: token,
    });

  let refreshed: Awaited<ReturnType<typeof refreshSession>> | null = null;
  if (!accessToken && refreshToken) {
    refreshed = await refreshSession(refreshToken, backendContext);
    if (refreshed.kind === "ok") accessToken = refreshed.tokens.accessToken;
  }
  if (!accessToken) {
    return refreshed?.kind === "unavailable"
      ? backendUnavailable()
      : unauthorized();
  }

  let upstream: Response;
  try {
    upstream = await call(accessToken);
    if (upstream.status === UNAUTHORIZED && refreshToken) {
      refreshed = await refreshSession(refreshToken, backendContext);
      if (refreshed.kind === "rejected") return unauthorized();
      if (refreshed.kind === "ok") {
        accessToken = refreshed.tokens.accessToken;
        upstream = await call(accessToken);
      }
    }
  } catch {
    return backendUnavailable();
  }

  const headers = new Headers();
  for (const name of PROXY_RESPONSE_HEADERS) {
    const value = upstream.headers.get(name);
    if (value) headers.set(name, value);
  }
  const response = new NextResponse(
    upstream.status === 204 ? null : await upstream.text(),
    { status: upstream.status, headers },
  );
  if (refreshed?.kind === "ok") {
    writeSessionCookies(response.cookies, refreshed.tokens);
  }
  return response;
}

export {
  handle as GET,
  handle as POST,
  handle as PUT,
  handle as PATCH,
  handle as DELETE,
};
