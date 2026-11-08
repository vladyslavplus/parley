const PLACEHOLDER_ORIGIN = "http://return-url.invalid";
const MAX_DECODE_ROUNDS = 3;

function decodeRepeatedly(value: string): string | null {
  let current = value;
  for (let round = 0; round < MAX_DECODE_ROUNDS; round += 1) {
    let next: string;
    try {
      next = decodeURIComponent(current);
    } catch {
      return null;
    }
    if (next === current) return current;
    current = next;
  }
  return current;
}

function isUnsafePath(value: string): boolean {
  return (
    !value.startsWith("/") ||
    value.startsWith("//") ||
    value.includes("\\") ||
    /[\u0000-\u001f\u007f]/.test(value)
  );
}

// Returns a same-origin relative path or null. Anything that could resolve to
// another origin (scheme, protocol-relative, backslash tricks, encoded forms)
// is rejected.
export function sanitizeReturnUrl(
  raw: string | null | undefined,
): string | null {
  if (!raw) return null;
  const decoded = decodeRepeatedly(raw);
  if (decoded === null) return null;
  if (isUnsafePath(raw) || isUnsafePath(decoded)) return null;

  let parsed: URL;
  try {
    parsed = new URL(raw, PLACEHOLDER_ORIGIN);
  } catch {
    return null;
  }
  if (parsed.origin !== PLACEHOLDER_ORIGIN) return null;
  return `${parsed.pathname}${parsed.search}${parsed.hash}`;
}
