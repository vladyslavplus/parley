import type { IntlError } from "next-intl";

const isDevelopment = () => process.env.NODE_ENV === "development";

// Development fails loudly so a missing key is fixed immediately; production
// renders the key path instead of breaking the page.
export function reportMissingMessage(error: IntlError): void {
  if (isDevelopment()) {
    throw error;
  }
}

export function messageFallback({
  namespace,
  key,
}: {
  namespace?: string;
  key: string;
}): string {
  return [namespace, key].filter(Boolean).join(".");
}
