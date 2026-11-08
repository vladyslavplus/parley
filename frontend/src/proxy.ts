import createIntlMiddleware from "next-intl/middleware";
import { NextResponse, type NextRequest } from "next/server";

import { ROUTES, ROUTE_PARAMS } from "@/constants/routes";
import { HEADERS, SESSION_COOKIES } from "@/constants/session";
import { routing } from "@/i18n/routing";
import { contextOf } from "@/lib/bff/server";
import { guardedLocale } from "@/lib/session/guard";
import { refreshSession } from "@/lib/session/refresh";
import { clearSessionCookies, writeSessionCookies } from "@/lib/session/tokens";

const handleIntl = createIntlMiddleware(routing);

function redirectToLogin(request: NextRequest, locale: string) {
  const url = request.nextUrl.clone();
  url.pathname = `/${locale}${ROUTES.login}`;
  url.search = "";
  url.searchParams.set(
    ROUTE_PARAMS.returnUrl,
    `${request.nextUrl.pathname}${request.nextUrl.search}`,
  );
  const response = NextResponse.redirect(url);
  clearSessionCookies(response.cookies);
  return response;
}

// The guarded area skips the locale middleware, so the locale header it would
// set is added here for the server-side translations of the page.
function passThrough(request: NextRequest, locale: string, cookie?: string) {
  const headers = new Headers(request.headers);
  headers.set(HEADERS.intlLocale, locale);
  if (cookie !== undefined) headers.set(HEADERS.cookie, cookie);
  return NextResponse.next({ request: { headers } });
}

export async function proxy(request: NextRequest) {
  const locale = guardedLocale(request.nextUrl.pathname);
  if (!locale) return handleIntl(request);

  const access = request.cookies.get(SESSION_COOKIES.access)?.value;
  if (access) return passThrough(request, locale);

  const refresh = request.cookies.get(SESSION_COOKIES.refresh)?.value;
  if (!refresh) return redirectToLogin(request, locale);

  const outcome = await refreshSession(refresh, {
    ...contextOf(request),
    acceptLanguage: locale,
  });
  if (outcome.kind === "rejected") return redirectToLogin(request, locale);
  if (outcome.kind === "unavailable") return passThrough(request, locale);

  // Pass the new access token to the rendering pass of this same request and
  // store the new pair on the response.
  request.cookies.set(SESSION_COOKIES.access, outcome.tokens.accessToken);
  request.cookies.set(SESSION_COOKIES.refresh, outcome.tokens.refreshToken);
  const response = passThrough(request, locale, request.cookies.toString());
  writeSessionCookies(response.cookies, outcome.tokens);
  return response;
}

export const config = {
  matcher: ["/((?!api|_next|_vercel|.*\\..*).*)"],
};
