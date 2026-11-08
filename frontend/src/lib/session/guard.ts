import { APP_AREA_SEGMENT } from "@/constants/routes";
import { isAppLocale, type AppLocale } from "@/i18n/routing";

// Returns the locale when the path is inside `/{locale}/app`, otherwise null.
export function guardedLocale(pathname: string): AppLocale | null {
  const [, locale, area] = pathname.split("/");
  if (!isAppLocale(locale) || area !== APP_AREA_SEGMENT) return null;
  return locale;
}
