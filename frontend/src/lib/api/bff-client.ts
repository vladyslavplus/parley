"use client";

import { API_ROUTES } from "@/constants/routes";
import { HEADERS, MEDIA_TYPES } from "@/constants/session";
import {
  apiErrorFromResponse,
  networkError,
  type ApiError,
} from "@/lib/errors";

import type { paths } from "./schema";

type Method = "GET" | "POST" | "PUT" | "DELETE";

export interface BffRequest {
  method?: Method;
  body?: unknown;
  query?: Record<string, string | number | null | undefined>;
  locale: string;
}

type BackendPath = keyof paths;

function withQuery(url: string, query: BffRequest["query"]): string {
  if (!query) return url;
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== null && value !== undefined) params.set(key, String(value));
  }
  const text = params.toString();
  return text ? `${url}?${text}` : url;
}

export async function bffFetch(
  url: string,
  { method = "GET", body, query, locale }: BffRequest,
): Promise<Response> {
  const headers: Record<string, string> = {
    [HEADERS.accept]: MEDIA_TYPES.json,
    [HEADERS.acceptLanguage]: locale,
  };
  if (body !== undefined) headers[HEADERS.contentType] = MEDIA_TYPES.json;
  let response: Response;
  try {
    response = await fetch(withQuery(url, query), {
      method,
      headers,
      body: body === undefined ? undefined : JSON.stringify(body),
      credentials: "same-origin",
    });
  } catch {
    throw networkError();
  }
  if (!response.ok) throw await apiErrorFromResponse(response);
  return response;
}

// Typed call to a data endpoint through the BFF proxy. `path` is checked
// against the generated OpenAPI paths; `T` is the response model.
export async function backendJson<T>(
  path: BackendPath,
  request: BffRequest,
  concretePath: string = path,
): Promise<T> {
  const response = await bffFetch(
    `${API_ROUTES.backend}${concretePath}`,
    request,
  );
  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export async function backendVoid(
  path: BackendPath,
  request: BffRequest,
  concretePath: string = path,
): Promise<void> {
  await bffFetch(`${API_ROUTES.backend}${concretePath}`, request);
}

export type { ApiError };
