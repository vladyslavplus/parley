import { LIMITS } from "@/constants/limits";

const LESS_THAN = "<";

// A p-value for display, with a fixed number of decimals in the viewer's
// locale. A value below the floor reads as a bound, never as 0.
export function formatPValue(value: number, locale: string): string {
  const format = new Intl.NumberFormat(locale, {
    minimumFractionDigits: LIMITS.pValueDigits,
    maximumFractionDigits: LIMITS.pValueDigits,
  });
  if (value < LIMITS.pValueFloor) {
    return `${LESS_THAN} ${format.format(LIMITS.pValueFloor)}`;
  }
  return format.format(value);
}
