import { API_ROUTES } from "@/constants/routes";
import { bffFetch, type BffRequest } from "@/lib/api/bff-client";

// A request to a notifications endpoint through the BFF proxy. The response type
// is supplied by the caller from the generated schema (see `types.ts`).
export async function notificationsJson<T>(
  concretePath: string,
  request: BffRequest,
): Promise<T> {
  const response = await bffFetch(
    `${API_ROUTES.backend}${concretePath}`,
    request,
  );
  return (await response.json()) as T;
}

// A request whose answer has no body (for example 204 No Content).
export async function notificationsRequest(
  concretePath: string,
  request: BffRequest,
): Promise<void> {
  await bffFetch(`${API_ROUTES.backend}${concretePath}`, request);
}
