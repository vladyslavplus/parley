"use client";

import {
  MutationCache,
  QueryCache,
  QueryClient,
  QueryClientProvider,
} from "@tanstack/react-query";
import {
  NextIntlClientProvider,
  useLocale,
  useMessages,
  useTranslations,
} from "next-intl";
import { ThemeProvider, useTheme } from "next-themes";
import { NuqsAdapter } from "nuqs/adapters/next/app";
import {
  useInsertionEffect,
  useState,
  type CSSProperties,
  type ReactNode,
} from "react";
import { Toaster } from "sonner";

import {
  AlertCircle,
  AlertTriangle,
  CheckCircle2,
  Info,
  Loader2,
} from "lucide-react";
import { LocaleCacheSync } from "@/components/locale-cache-sync";
import { ROUTES, ROUTE_PARAMS } from "@/constants/routes";
import { LIMITS } from "@/constants/limits";
import { THEME_STORAGE_KEY } from "@/constants/theme";
import { messageFallback, reportMissingMessage } from "@/i18n/missing";
import { ApiError } from "@/lib/errors";

// Guard next-themes inline script warning patch so it installs exactly once across HMR
declare global {
  var __parleyConsoleErrorPatched: boolean | undefined;
  var __parleyApplyTheme: (() => void) | undefined;
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
  if (typeof window === "undefined") return;
  if (window.location.pathname.includes(ROUTES.login)) return;
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
        queries: {
          retry: false,
          refetchOnWindowFocus: false,
          staleTime: LIMITS.queryStaleTimeMs,
          gcTime: LIMITS.queryGcTimeMs,
        },
      },
    });
  });
  return (
    <QueryClientProvider client={queryClient}>
      <LocaleCacheSync />
      {children}
    </QueryClientProvider>
  );
}

function ThemeRootSync({ locale }: { locale: string }) {
  const { resolvedTheme } = useTheme();
  useInsertionEffect(() => {
    if (typeof window !== "undefined") {
      window.__parleyApplyTheme?.();
    }
  }, [locale, resolvedTheme]);
  return null;
}

export function ThemedToaster() {
  const { resolvedTheme } = useTheme();
  const tToast = useTranslations("common.toast");
  return (
    <Toaster
      position="top-right"
      theme={(resolvedTheme as "light" | "dark" | "system") || "system"}
      offset={`${LIMITS.toastTopOffsetPx}px`}
      visibleToasts={LIMITS.toastMaxVisible}
      duration={LIMITS.toastDurationMs}
      closeButton
      containerAriaLabel={tToast("regionLabel")}
      style={
        {
          "--width": `${LIMITS.toastMaxWidthPx}px`,
          "--normal-bg": "var(--muted)",
          "--normal-border": "var(--border)",
          "--normal-text": "var(--muted-foreground)",
          "--normal-bg-hover": "var(--accent)",
          "--normal-border-hover": "var(--ring)",
        } as CSSProperties
      }
      icons={{
        success: <CheckCircle2 className="h-4 w-4 text-success shrink-0" />,
        info: <Info className="h-4 w-4 text-info shrink-0" />,
        warning: <AlertTriangle className="h-4 w-4 text-warning shrink-0" />,
        error: <AlertCircle className="h-4 w-4 text-destructive shrink-0" />,
        loading: (
          <Loader2 className="h-4 w-4 animate-spin text-muted-foreground shrink-0" />
        ),
      }}
      toastOptions={{
        unstyled: true,
        duration: LIMITS.toastDurationMs,
        closeButtonAriaLabel: tToast("close"),
        classNames: {
          toast:
            "group relative w-[var(--width)] max-w-[var(--width)] rounded-[var(--radius-card)] border border-border bg-card px-3.5 py-2.5 shadow-floating text-foreground font-sans text-xs flex items-center gap-2.5 transition-all",
          icon: "flex items-center justify-center shrink-0",
          content: "flex flex-col justify-center gap-0.5 flex-1 min-w-0",
          title: "font-medium text-xs text-foreground leading-snug",
          description: "text-xs text-muted-foreground leading-snug",
          actionButton:
            "font-semibold text-xs text-primary hover:text-primary/80 transition-colors underline-offset-2 hover:underline ml-auto shrink-0 bg-transparent border-0 px-1.5 py-0.5 rounded-[var(--radius-control)] focus-visible:outline-2 focus-visible:outline-ring shadow-none cursor-pointer",
          cancelButton:
            "text-xs text-muted-foreground hover:text-foreground shrink-0 cursor-pointer",
          closeButton:
            "absolute -top-1.5 -right-1.5 z-10 flex h-5 w-5 items-center justify-center rounded-full border border-border bg-muted text-muted-foreground shadow-xs transition-all hover:bg-accent hover:text-foreground hover:border-ring focus-visible:outline-2 focus-visible:outline-ring focus-visible:opacity-100 opacity-0 group-hover:opacity-100 group-focus-within:opacity-100 [@media(hover:none),(pointer:coarse)]:opacity-100 cursor-pointer p-0",
          error: "border-destructive/30 bg-card",
          success: "border-success/30 bg-card",
          warning: "border-warning/30 bg-card",
          info: "border-info/30 bg-card",
        },
      }}
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
      enableColorScheme
    >
      <ThemeRootSync locale={locale} />
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
