"use client";

import { useTranslations } from "next-intl";

import { describeError, toApiError } from "@/lib/errors";

// Localized text for any caught error: the server detail when there is one,
// otherwise the generic message for the error kind.
export function useErrorText(): (error: unknown) => string {
  const tKinds = useTranslations("errors.kinds");
  return (error) =>
    describeError(toApiError(error), (kind) =>
      tKinds(kind as Parameters<typeof tKinds>[0]),
    );
}
