import createFetchClient from "openapi-fetch";
import createReactQueryClient from "openapi-react-query";

import { requireApiBaseUrl } from "./env";
import type { paths } from "./schema";

// Creates typed API clients. `baseUrl` is explicit when the caller already
// knows it; otherwise it is read from the server environment (`API_BASE_URL`).
export function createApiClients(options: { baseUrl?: string } = {}) {
  const baseUrl = options.baseUrl ?? requireApiBaseUrl();
  const fetchClient = createFetchClient<paths>({ baseUrl });
  const $api = createReactQueryClient(fetchClient);
  return { fetchClient, $api };
}
