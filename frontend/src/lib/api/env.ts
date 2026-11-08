// Server-side environment access. `API_BASE_URL` is read on the server
// (BFF) and is never inlined into browser bundles; client components that
// need an API client must pass an explicit `baseUrl` instead.

// True when the BFF runs behind an ingress/reverse proxy that appends the real
// peer address to X-Forwarded-For (set TRUSTED_INGRESS=true only then).
export function isTrustedIngress(): boolean {
  return process.env.TRUSTED_INGRESS === "true";
}

export function requireApiBaseUrl(): string {
  const value = process.env.API_BASE_URL;
  if (!value) {
    throw new Error("API_BASE_URL is not configured");
  }
  return value.replace(/\/+$/, "");
}
