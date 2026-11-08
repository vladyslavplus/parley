import {
  BACKEND_PROXY_ROUTES,
  GUID_PATTERN,
  ROUTE_ID_PLACEHOLDER,
  type ProxyMethod,
} from "@/constants/proxy-routes";

export type ProxyMatch =
  | { kind: "allowed" }
  | { kind: "method-not-allowed"; allowed: ProxyMethod[] }
  | { kind: "not-found" };

function matchesTemplate(template: string, segments: string[]): boolean {
  const parts = template.split("/");
  if (parts.length !== segments.length) return false;
  return parts.every((part, index) =>
    part === ROUTE_ID_PLACEHOLDER
      ? GUID_PATTERN.test(segments[index] ?? "")
      : part === segments[index],
  );
}

// Literal segments must match exactly and ids must be GUIDs, so dot segments
// (plain or percent-encoded) and unknown endpoints never match.
export function matchProxyRoute(
  method: string,
  segments: string[],
): ProxyMatch {
  const onPath = BACKEND_PROXY_ROUTES.filter((route) =>
    matchesTemplate(route.template, segments),
  );
  if (onPath.length === 0) return { kind: "not-found" };
  if (onPath.some((route) => route.method === method)) {
    return { kind: "allowed" };
  }
  return {
    kind: "method-not-allowed",
    allowed: onPath.map((route) => route.method),
  };
}
