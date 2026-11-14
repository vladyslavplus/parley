"use client";

import { useLocale, useTranslations } from "next-intl";

import { usePathname, useRouter } from "@/i18n/navigation";
import { LOCALES, type AppLocale } from "@/i18n/routing";
import { cn } from "@/lib/utils";

export function LocaleToggle() {
  const currentLocale = useLocale() as AppLocale;
  const pathname = usePathname();
  const router = useRouter();
  const t = useTranslations("common");

  function switchLocale(locale: AppLocale) {
    if (locale !== currentLocale) {
      const search =
        typeof window !== "undefined" ? window.location.search : "";
      const target = search ? `${pathname}${search}` : pathname;
      router.replace(target, { locale });
    }
  }

  return (
    <div
      role="group"
      aria-label={t("language")}
      className="inline-flex rounded-[var(--radius-control)] border border-border p-0.5 bg-card"
    >
      {LOCALES.map((locale) => {
        const isActive = locale === currentLocale;
        return (
          <button
            key={locale}
            type="button"
            lang={locale}
            onClick={() => switchLocale(locale)}
            className={cn(
              "h-7 px-2 text-xs uppercase font-medium rounded-[var(--radius-control)] transition-colors focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring",
              isActive
                ? "bg-muted text-foreground font-semibold"
                : "text-muted-foreground hover:text-foreground",
            )}
            aria-pressed={isActive}
          >
            {locale}
          </button>
        );
      })}
    </div>
  );
}
