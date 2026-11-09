"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale } from "next-intl";
import { useEffect, useRef } from "react";

// The backend localizes some data (default pipeline and stage names) by the
// request locale, and query keys do not carry it. When the locale changes,
// mark every cached query stale and refetch the mounted ones so the UI never
// keeps the previous language.
export function LocaleCacheSync() {
  const locale = useLocale();
  const queryClient = useQueryClient();
  const previous = useRef(locale);

  useEffect(() => {
    if (previous.current === locale) return;
    previous.current = locale;
    void queryClient.invalidateQueries();
  }, [locale, queryClient]);

  return null;
}
