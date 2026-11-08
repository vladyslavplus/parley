"use client";

import { AlertTriangle, RotateCcw } from "lucide-react";
import Link from "next/link";
import { useEffect } from "react";

import { fontDisplay, fontMono, fontSans } from "@/app/fonts";
import { THEME_STORAGE_KEY } from "@/constants/theme";
import { cn } from "@/lib/utils";

import errorsEn from "../../../messages/en/errors.json";
import errorsUk from "../../../messages/uk/errors.json";

import "@/app/globals.css";

export interface GlobalStatusPageProps {
  status: "404" | "500";
  onRetry?: () => void;
}

const themeScript = `(function(){try{var t=localStorage.getItem('${THEME_STORAGE_KEY}');var d=t==='dark'||((!t||t==='system')&&window.matchMedia('(prefers-color-scheme: dark)').matches);if(d){document.documentElement.classList.add('dark');}}catch(e){}})()`;

function ClientThemeSync() {
  useEffect(() => {
    try {
      const t = localStorage.getItem(THEME_STORAGE_KEY);
      const isDark =
        t === "dark" ||
        ((!t || t === "system") &&
          window.matchMedia("(prefers-color-scheme: dark)").matches);
      if (isDark) {
        document.documentElement.classList.add("dark");
      }
    } catch {
      // Ignore localStorage access failures
    }
  }, []);

  return null;
}

export function GlobalStatusPage({ status, onRetry }: GlobalStatusPageProps) {
  const is404 = status === "404";

  return (
    <html
      lang="en"
      className={cn(fontSans.variable, fontDisplay.variable, fontMono.variable)}
      suppressHydrationWarning
    >
      {is404 ? (
        /* eslint-disable-next-line @next/next/no-head-element */
        <head>
          <script
            dangerouslySetInnerHTML={{
              __html: themeScript,
            }}
          />
        </head>
      ) : (
        /* eslint-disable-next-line @next/next/no-head-element */
        <head />
      )}
      <body className="min-h-screen bg-background font-sans text-foreground antialiased flex flex-col items-center justify-center p-6 text-center">
        {!is404 && <ClientThemeSync />}
        <main
          role={is404 ? "main" : "alert"}
          className="max-w-md w-full rounded-[var(--radius-card)] border border-border bg-card p-8 shadow-floating space-y-5"
        >
          <div className="flex justify-center">
            {is404 ? (
              <div className="flex h-10 w-10 items-center justify-center rounded-[var(--radius-control)] bg-primary text-primary-foreground font-mono font-bold text-lg select-none">
                P
              </div>
            ) : (
              <div className="flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10 text-destructive">
                <AlertTriangle className="h-6 w-6" aria-hidden="true" />
              </div>
            )}
          </div>

          <div className="space-y-1.5">
            {is404 && (
              <span className="inline-block rounded-full bg-muted px-2.5 py-0.5 text-xs font-mono font-medium text-muted-foreground">
                {errorsEn.notFound.badge}
              </span>
            )}
            <h1 className="text-xl font-semibold tracking-tight text-foreground">
              {is404 ? (
                <>
                  {errorsEn.notFound.title} /{" "}
                  <span lang="uk">{errorsUk.notFound.title}</span>
                </>
              ) : (
                <>
                  {errorsEn.global.title} /{" "}
                  <span lang="uk">{errorsUk.global.title}</span>
                </>
              )}
            </h1>
            <p className="text-xs text-muted-foreground leading-relaxed">
              {is404 ? (
                <>
                  {errorsEn.notFound.description} /{" "}
                  <span lang="uk">{errorsUk.notFound.description}</span>
                </>
              ) : (
                <>
                  {errorsEn.global.description} /{" "}
                  <span lang="uk">{errorsUk.global.description}</span>
                </>
              )}
            </p>
          </div>

          <div className="pt-2">
            {is404 ? (
              <Link
                href="/"
                className="inline-flex h-9 items-center justify-center rounded-[var(--radius-control)] bg-primary px-4 text-xs font-medium text-primary-foreground shadow-sm hover:opacity-90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring transition-opacity"
              >
                {errorsEn.notFound.returnHome} /{" "}
                <span lang="uk">{errorsUk.notFound.returnHome}</span>
              </Link>
            ) : (
              <button
                type="button"
                onClick={() => onRetry?.()}
                className="inline-flex h-9 items-center justify-center gap-2 rounded-[var(--radius-control)] bg-primary px-4 text-xs font-medium text-primary-foreground shadow-sm hover:opacity-90 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring transition-opacity cursor-pointer"
              >
                <RotateCcw className="h-4 w-4" />
                <span>
                  {errorsEn.global.reload} /{" "}
                  <span lang="uk">{errorsUk.global.reload}</span>
                </span>
              </button>
            )}
          </div>
        </main>
      </body>
    </html>
  );
}
