import { defineRouting } from "next-intl/routing";

export const LOCALES = ["uk", "en"] as const;
export type AppLocale = (typeof LOCALES)[number];
export const DEFAULT_LOCALE: AppLocale = "uk";

export const routing = defineRouting({
  locales: LOCALES,
  defaultLocale: DEFAULT_LOCALE,
  localePrefix: "always",
});

export function isAppLocale(value: string | undefined): value is AppLocale {
  return (LOCALES as readonly string[]).includes(value ?? "");
}
