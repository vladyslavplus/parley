import { formatCurrency, formatNumber } from "@/lib/formatters";

/**
 * Uplift is a probability difference; the interface speaks in percentage
 * points, one decimal at most, with an explicit sign for a change.
 */
export function formatPoints(
  value: number,
  locale: string,
  fixedDigits = false,
): string {
  return new Intl.NumberFormat(locale, {
    signDisplay: "exceptZero",
    minimumFractionDigits: fixedDigits ? 1 : 0,
    maximumFractionDigits: 1,
  }).format(value);
}

/** A probability as a whole percentage: 0.42 -> "42 %" in the current locale. */
export function formatPercent(value: number, locale: string): string {
  return `${formatNumber(value * 100, locale, { maximumFractionDigits: 0 })} %`;
}

export interface Horizon {
  unit: "days" | "months";
  count: number;
}

/** The model horizon in the unit a person would say: 365 days -> 12 months. */
export function horizonOf(days: number): Horizon {
  const rounded = Math.round(days);
  if (rounded >= 30) return { unit: "months", count: Math.round(rounded / 30) };
  return { unit: "days", count: rounded };
}

/** Money of the model horizon, or a plain number when the API sent no currency. */
export function formatExpectedValue(
  value: number,
  currency: string | null,
  locale: string,
): string {
  if (currency) {
    return formatCurrency(value, currency, locale, {
      currencyDisplay: "narrowSymbol",
      minimumFractionDigits: 0,
      maximumFractionDigits: 0,
    });
  }
  return formatNumber(value, locale, { maximumFractionDigits: 0 });
}

/** A threshold on the points scale: a minus sign for a negative one, no plus sign. */
export function formatThresholdPoints(value: number, locale: string): string {
  return new Intl.NumberFormat(locale, {
    signDisplay: "auto",
    maximumFractionDigits: 1,
  }).format(value);
}
