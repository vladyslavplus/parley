import { API_ROUTES } from "@/constants/routes";
import { bffFetch, type BffRequest } from "@/lib/api/bff-client";

// Request to an insights or dashboard endpoint through the BFF proxy. The
// response model is supplied by the caller until the generated types include
// these paths (see `types.ts`).
export async function insightsJson<T>(
  concretePath: string,
  request: BffRequest,
): Promise<T> {
  const response = await bffFetch(
    `${API_ROUTES.backend}${concretePath}`,
    request,
  );
  return (await response.json()) as T;
}
