import { DEFAULT_LOCALE, type AppLocale } from "@/i18n/routing";

export interface PluralForms {
  one: string;
  few?: string;
  many?: string;
  other: string;
}

export function formatDate(
  value: Date | string | number,
  locale: string = DEFAULT_LOCALE,
  options?: Intl.DateTimeFormatOptions,
): string {
  const date = typeof value === "object" ? value : new Date(value);
  return new Intl.DateTimeFormat(locale, {
    year: "numeric",
    month: "short",
    day: "numeric",
    ...options,
  }).format(date);
}

export function formatDateTime(
  value: Date | string | number,
  locale: string = DEFAULT_LOCALE,
  timeZone?: string,
  options?: Intl.DateTimeFormatOptions,
): string {
  const date = typeof value === "object" ? value : new Date(value);
  return new Intl.DateTimeFormat(locale, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZone,
    ...options,
  }).format(date);
}

export function formatNumber(
  value: number,
  locale: string = DEFAULT_LOCALE,
  options?: Intl.NumberFormatOptions,
): string {
  return new Intl.NumberFormat(locale, options).format(value);
}

export function formatCurrency(
  amount: number,
  currency: string = "UAH",
  locale: string = DEFAULT_LOCALE,
  options?: Intl.NumberFormatOptions,
): string {
  return new Intl.NumberFormat(locale, {
    style: "currency",
    currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
    ...options,
  }).format(amount);
}

export function formatPlural(
  count: number,
  locale: string = DEFAULT_LOCALE,
  forms: PluralForms,
): string {
  const rules = new Intl.PluralRules(locale as AppLocale);
  const category = rules.select(count);

  const template =
    (category === "one" && forms.one) ||
    (category === "few" && forms.few) ||
    (category === "many" && forms.many) ||
    forms.other;

  return template.replace(/\{count\}/g, formatNumber(count, locale));
}
