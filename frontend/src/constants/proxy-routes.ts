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
  { method: "GET", template: "contacts" },
  { method: "POST", template: "contacts" },
  { method: "GET", template: "contacts/{id}" },
  { method: "PUT", template: "contacts/{id}" },
  { method: "DELETE", template: "contacts/{id}" },
  { method: "POST", template: "contacts/{id}/restore" },
  { method: "POST", template: "contacts/bulk-owner" },
  { method: "POST", template: "contacts/bulk-delete" },
  { method: "POST", template: "contacts/{id}/tags" },
  { method: "DELETE", template: "contacts/{id}/tags/{id}" },
  { method: "POST", template: "contacts/exports" },
  { method: "GET", template: "contacts/exports/{id}" },
  { method: "GET", template: "contacts/exports/{id}/download" },
  { method: "GET", template: "companies" },
  { method: "POST", template: "companies" },
  { method: "GET", template: "companies/{id}" },
  { method: "PUT", template: "companies/{id}" },
  { method: "DELETE", template: "companies/{id}" },
  { method: "POST", template: "companies/{id}/restore" },
  { method: "POST", template: "companies/bulk-owner" },
  { method: "POST", template: "companies/bulk-delete" },
  { method: "GET", template: "tags" },
  { method: "POST", template: "tags" },
  { method: "PUT", template: "tags/{id}" },
  { method: "DELETE", template: "tags/{id}" },
  { method: "GET", template: "custom-fields" },
  { method: "POST", template: "custom-fields" },
  { method: "PUT", template: "custom-fields/{id}" },
  { method: "DELETE", template: "custom-fields/{id}" },
  { method: "POST", template: "custom-fields/{id}/archive" },
  { method: "GET", template: "search" },
];
