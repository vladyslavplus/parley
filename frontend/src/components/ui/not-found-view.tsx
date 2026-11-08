"use client";

import { Compass, FileQuestion } from "lucide-react";
import Link from "next/link";
import { useLocale, useTranslations } from "next-intl";

import { buttonVariants } from "@/components/ui/button";
import { ROUTES } from "@/constants/routes";
import { cn } from "@/lib/utils";

export interface NotFoundViewProps {
  inApp?: boolean;
  className?: string;
  isAuthenticated?: boolean;
}

export function NotFoundView({
  inApp = false,
  className,
  isAuthenticated = false,
}: NotFoundViewProps) {
  const t = useTranslations("errors.notFound");
  const locale = useLocale();

  const appHref = `/${locale}${ROUTES.settingsProfile}`;
  const homeHref = `/${locale}${ROUTES.login}`;

  if (inApp) {
    return (
      <div
        role="region"
        aria-labelledby="in-app-not-found-title"
        className={cn(
          "flex min-h-[50vh] flex-col items-center justify-center p-6 text-center",
          className,
        )}
      >
        <div className="mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-muted text-muted-foreground">
          <FileQuestion className="h-6 w-6" aria-hidden="true" />
        </div>
        <div className="mb-2">
          <span className="inline-block rounded-full bg-muted px-2.5 py-0.5 text-xs font-mono font-medium text-muted-foreground">
            {t("badge")}
          </span>
        </div>
        <h2
          id="in-app-not-found-title"
          className="text-lg font-semibold tracking-tight text-foreground"
        >
          {t("inAppTitle")}
        </h2>
        <p className="mt-1 max-w-md text-xs text-muted-foreground leading-relaxed">
          {t("inAppDescription")}
        </p>
        <div className="mt-6 flex items-center justify-center gap-3">
          <Link
            href={appHref}
            className={buttonVariants({
              variant: "primary",
              className: "gap-2 text-xs",
            })}
          >
            <Compass className="h-4 w-4" />
            {t("returnApp")}
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div
      role="region"
      aria-labelledby="not-found-title"
      className={cn(
        "flex min-h-screen flex-col items-center justify-center p-6 text-center",
        className,
      )}
    >
      <div className="w-full max-w-md rounded-[var(--radius-card)] border border-border bg-card p-8 shadow-floating space-y-6">
        <div className="flex justify-center">
          <div className="flex h-10 w-10 items-center justify-center rounded-[var(--radius-control)] bg-primary text-primary-foreground font-mono font-bold text-lg select-none">
            P
          </div>
        </div>

        <div className="space-y-2">
          <span className="inline-block rounded-full bg-muted px-2.5 py-0.5 text-xs font-mono font-medium text-muted-foreground">
            {t("badge")}
          </span>
          <h1
            id="not-found-title"
            className="text-xl font-semibold tracking-tight text-foreground"
          >
            {t("title")}
          </h1>
          <p className="text-xs text-muted-foreground leading-relaxed">
            {t("description")}
          </p>
        </div>

        <div className="flex flex-col sm:flex-row items-center justify-center gap-2 pt-2">
          {isAuthenticated ? (
            <>
              <Link
                href={appHref}
                className={buttonVariants({
                  variant: "primary",
                  className: "w-full sm:w-auto gap-2 text-xs",
                })}
              >
                <Compass className="h-4 w-4" />
                {t("returnApp")}
              </Link>
              <Link
                href={homeHref}
                className={buttonVariants({
                  variant: "secondary",
                  className: "w-full sm:w-auto text-xs",
                })}
              >
                {t("returnHome")}
              </Link>
            </>
          ) : (
            <Link
              href={homeHref}
              className={buttonVariants({
                variant: "primary",
                className: "w-full sm:w-auto text-xs",
              })}
            >
              {t("returnHome")}
            </Link>
          )}
        </div>
      </div>
    </div>
  );
}
