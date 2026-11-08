import { describe, expect, it } from "vitest";

import {
  formatCurrency,
  formatDate,
  formatDateTime,
  formatNumber,
  formatPlural,
} from "./formatters";

describe("formatters", () => {
  it("formats dates and datetimes with locale and timezone", () => {
    const iso = "2026-06-01T12:00:00Z";
    const formattedEn = formatDate(iso, "en");
    expect(formattedEn).toContain("2026");

    const formattedTime = formatDateTime(iso, "en", "UTC", { hour12: false });
    expect(formattedTime).toContain("12:00");
  });

  it("formats numbers and currency for uk and en", () => {
    const num = 1234567.89;
    const formattedNumUk = formatNumber(num, "uk");
    // Ukrainian uses space as thousands separator
    expect(formattedNumUk).toMatch(/1.*234.*567/);

    const currencyUk = formatCurrency(1500, "UAH", "uk");
    expect(currencyUk).toContain("1");
    expect(currencyUk).toMatch(/грн|UAH|₴/);

    const currencyUsd = formatCurrency(1500, "USD", "en");
    expect(currencyUsd).toContain("$1,500.00");
  });

  it("handles Ukrainian 4-form ICU plurals properly", () => {
    const forms = {
      one: "{count} контакт",
      few: "{count} контакти",
      many: "{count} контактів",
      other: "{count} контактів",
    };

    expect(formatPlural(1, "uk", forms)).toBe("1 контакт");
    expect(formatPlural(2, "uk", forms)).toBe("2 контакти");
    expect(formatPlural(4, "uk", forms)).toBe("4 контакти");
    expect(formatPlural(5, "uk", forms)).toBe("5 контактів");
    expect(formatPlural(21, "uk", forms)).toBe("21 контакт");
    expect(formatPlural(22, "uk", forms)).toBe("22 контакти");
    expect(formatPlural(100, "uk", forms)).toBe("100 контактів");
  });

  it("handles English plurals properly", () => {
    const forms = {
      one: "{count} contact",
      other: "{count} contacts",
    };

    expect(formatPlural(1, "en", forms)).toBe("1 contact");
    expect(formatPlural(5, "en", forms)).toBe("5 contacts");
  });
});
