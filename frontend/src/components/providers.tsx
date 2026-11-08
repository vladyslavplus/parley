"use client";

import {
  MutationCache,
  QueryCache,
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";
import { NextIntlClientProvider, useLocale, useMessages } from "next-intl";
import { ThemeProvider, useTheme } from "next-themes";
import { NuqsAdapter } from "nuqs/adapters/next/app";
import { useState, type ReactNode } from "react";
import { Toaster } from "sonner";

import { ROUTES, ROUTE_PARAMS } from "@/constants/routes";
import { THEME_STORAGE_KEY } from "@/constants/theme";
import { messageFallback, reportMissingMessage } from "@/i18n/missing";
import { ApiError } from "@/lib/errors";

// Guard next-themes inline script warning patch so it installs exactly once across HMR
declare global {
  var __parleyConsoleErrorPatched: boolean | undefined;
}

if (
  typeof window !== "undefined" &&
  process.env.NODE_ENV === "development" &&
  !globalThis.__parleyConsoleErrorPatched
) {
  globalThis.__parleyConsoleErrorPatched = true;
  const originalConsoleError = console.error;
  console.error = (...args: unknown[]) => {
    if (
      typeof args[0] === "string" &&
      args[0].includes(
        "Encountered a script tag while rendering React component",
      )
    ) {
      return;
    }
    originalConsoleError.apply(console, args);
  };
}

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

function ThemedToaster() {
  const { resolvedTheme } = useTheme();
  return (
    <Toaster
      position="bottom-right"
      theme={(resolvedTheme as "light" | "dark" | "system") || "system"}
    />
  );
}

export function Providers({ children }: { children: ReactNode }) {
  const locale = useLocale();
  const messages = useMessages();
  return (
    <ThemeProvider
      attribute="class"
      storageKey={THEME_STORAGE_KEY}
      defaultTheme="system"
      enableSystem
      disableTransitionOnChange
      enableColorScheme={false}
    >
      <NextIntlClientProvider
        locale={locale}
        messages={messages}
        onError={reportMissingMessage}
        getMessageFallback={messageFallback}
      >
        <NuqsAdapter>
          <QueryProvider>{children}</QueryProvider>
        </NuqsAdapter>
        <ThemedToaster />
      </NextIntlClientProvider>
    </ThemeProvider>
  );
}
