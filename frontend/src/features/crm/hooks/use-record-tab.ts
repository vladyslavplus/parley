"use client";

import { parseAsStringLiteral, useQueryState } from "nuqs";

export const RECORD_TAB_PARAM = "tab";

// The active record tab lives in the URL so reloads and deep links keep it.
export function useRecordTab<const K extends string>(
  keys: readonly K[],
  defaultKey: NoInfer<K>,
) {
  return useQueryState(
    RECORD_TAB_PARAM,
    parseAsStringLiteral(keys).withDefault(defaultKey),
  );
}
