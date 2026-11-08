"use client";

import {
  MutationCache,
  QueryCache,
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";
import { NextIntlClientProvider, useLocale, useMessages } from "next-intl";
import { useState, type ReactNode } from "react";

import { ROUTES, ROUTE_PARAMS } from "@/constants/routes";
import { messageFallback, reportMissingMessage } from "@/i18n/missing";
import { ApiError } from "@/lib/errors";

function redirectToLogin(locale: string) {
  const returnUrl = `${window.location.pathname}${window.location.search}`;
  const target = new URL(`/${locale}${ROUTES.login}`, window.location.origin);
  target.searchParams.set(ROUTE_PARAMS.returnUrl, returnUrl);
  window.location.assign(target.toString());
}

function QueryProvider({ children }: { children: ReactNode }) {
  const locale = useLocale();
  const [queryClient] = useState(() => {
    const onError = (error: unknown) => {
      if (error instanceof ApiError && error.kind === "unauthorized") {
        redirectToLogin(locale);
      }
    };
    return new QueryClient({
      queryCache: new QueryCache({ onError }),
      mutationCache: new MutationCache({ onError }),
      defaultOptions: {
        queries: { retry: false, refetchOnWindowFocus: false },
      },
    });
  });
  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

export function Providers({ children }: { children: ReactNode }) {
  const locale = useLocale();
  const messages = useMessages();
  return (
    <NextIntlClientProvider
      locale={locale}
      messages={messages}
      onError={reportMissingMessage}
      getMessageFallback={messageFallback}
    >
      <QueryProvider>{children}</QueryProvider>
    </NextIntlClientProvider>
  );
}
