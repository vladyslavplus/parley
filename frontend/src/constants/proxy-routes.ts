export type ProxyMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

export interface ProxyRoute {
  method: ProxyMethod;
  // Path template relative to `API_ROUTES.backend`; `{id}` matches a GUID.
  template: string;
}

export const ROUTE_ID_PLACEHOLDER = "{id}";

export const GUID_PATTERN =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// Exact backend endpoints the browser may reach through the generic proxy.
// Anything else (token endpoints, organization switch, invitation accept,
// traversal attempts) is answered with 404 before the backend is called.
export const BACKEND_PROXY_ROUTES: readonly ProxyRoute[] = [
  { method: "GET", template: "me" },
  { method: "GET", template: "me/organizations" },
  { method: "PUT", template: "me/profile" },
  { method: "GET", template: "organizations/{id}" },
  { method: "PUT", template: "organizations/{id}" },
  { method: "GET", template: "members" },
  { method: "PUT", template: "members/{id}/role" },
  { method: "PUT", template: "members/{id}/team" },
  { method: "POST", template: "members/{id}/deactivate" },
  { method: "GET", template: "teams" },
  { method: "POST", template: "teams" },
  { method: "GET", template: "teams/{id}" },
  { method: "PUT", template: "teams/{id}" },
  { method: "DELETE", template: "teams/{id}" },
  { method: "GET", template: "invitations" },
  { method: "POST", template: "invitations" },
  { method: "POST", template: "invitations/{id}/revoke" },
  { method: "GET", template: "audit-events" },
];
