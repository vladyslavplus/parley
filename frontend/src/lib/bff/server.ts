import { isIP } from "node:net";

import { HEADERS, MEDIA_TYPES } from "@/constants/session";

import { isTrustedIngress, requireApiBaseUrl } from "@/lib/api/env";
import type { paths } from "@/lib/api/schema";

export type PostPath = {
  [P in keyof paths]: paths[P] extends { post: object } ? P : never;
}[keyof paths];

// Per-request values the BFF passes on to the backend.
export interface BackendContext {
  acceptLanguage?: string | null;
  // End-user address; the backend rate-limits by it.
  clientAddress?: string | null;
}

export interface BackendRequest extends BackendContext {
  method: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  path: string;
  search?: string;
  body?: string;
  accessToken?: string;
  idempotencyKey?: string | null;
}

const FORWARDED_FOR_SEPARATOR = ",";

// The end-user address of an incoming browser request. Only a trusted ingress
// (TRUSTED_INGRESS=true) appends the real peer as the last X-Forwarded-For
// entry; without one the header is client-controlled and must not be used, so
// no address is forwarded and the backend sees the BFF address.
export function clientAddressOf(request: Request): string | null {
  if (!isTrustedIngress()) return null;
  const header = request.headers.get(HEADERS.forwardedFor);
  if (!header) return null;
  const last = header.split(FORWARDED_FOR_SEPARATOR).at(-1)?.trim() ?? "";
  return isIP(last) === 0 ? null : last;
}

export function contextOf(request: Request): BackendContext {
  return {
    acceptLanguage: request.headers.get(HEADERS.acceptLanguage),
    clientAddress: clientAddressOf(request),
  };
}

// Server-side call to the .NET API. Forwards the bearer token, the UI
// language and the resolved end-user address; never a client-supplied header.
export function backendFetch(request: BackendRequest): Promise<Response> {
  const headers = new Headers({ [HEADERS.accept]: MEDIA_TYPES.json });
  if (request.accessToken) {
    headers.set(HEADERS.authorization, `Bearer ${request.accessToken}`);
  }
  if (request.acceptLanguage) {
    headers.set(HEADERS.acceptLanguage, request.acceptLanguage);
  }
  if (request.clientAddress) {
    headers.set(HEADERS.forwardedFor, request.clientAddress);
  }
  if (request.body !== undefined) {
    headers.set(HEADERS.contentType, MEDIA_TYPES.json);
  }
  if (request.idempotencyKey) {
    headers.set(HEADERS.idempotencyKey, request.idempotencyKey);
  }
  return fetch(`${requireApiBaseUrl()}${request.path}${request.search ?? ""}`, {
    method: request.method,
    headers,
    body: request.body,
    cache: "no-store",
    redirect: "manual",
  });
}

export function postBackend(
  path: PostPath,
  options: Omit<BackendRequest, "method" | "path">,
): Promise<Response> {
  return backendFetch({ ...options, method: "POST", path });
}
