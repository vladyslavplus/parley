import { describe, expect, it } from "vitest";

import en from "../../../../messages/en/notifications.json";
import uk from "../../../../messages/uk/notifications.json";
import { ERROR_CODES } from "@/constants/error-codes";
import { LIMITS } from "@/constants/limits";
import {
  TELEGRAM_ERROR_KEYS,
  formatCountdown,
  isLinkCodeActive,
  linkPollInterval,
  secondsUntil,
  shouldPollLink,
  telegramErrorKey,
  telegramVisible,
} from "@/features/notifications/lib/telegram";

// Every Telegram code the link endpoints can return must have a friendly message
// in both locales, or the page would show raw codes.
const BACKEND_TELEGRAM_CODES = [
  "telegram.not_configured",
  "telegram.link.already_exists",
  "telegram.link_code.rate_limited",
  "telegram.link_code.invalid",
  "telegram.chat.already_linked",
  "telegram.link.not_found",
];

function lookup(messages: Record<string, unknown>, dotted: string) {
  let node: unknown = messages;
  for (const part of dotted.split(".")) {
    if (typeof node !== "object" || node === null) return undefined;
    node = (node as Record<string, unknown>)[part];
  }
  return typeof node === "string" ? node : undefined;
}

describe("Telegram visibility", () => {
  it("shows Telegram only when the server reports it enabled", () => {
    expect(telegramVisible({ enabled: true })).toBe(true);
    expect(telegramVisible({ enabled: false })).toBe(false);
    expect(telegramVisible(undefined)).toBe(false);
  });
});

describe("Telegram error messages", () => {
  it("maps every backend Telegram code to a message in both locales", () => {
    for (const code of BACKEND_TELEGRAM_CODES) {
      const key = telegramErrorKey(code);
      expect(key, code).not.toBeNull();
      expect(lookup(en, key as string), `en ${code}`).toBeTruthy();
      expect(lookup(uk, key as string), `uk ${code}`).toBeTruthy();
    }
    expect(Object.keys(TELEGRAM_ERROR_KEYS)).toHaveLength(
      BACKEND_TELEGRAM_CODES.length,
    );
    expect(telegramErrorKey("telegram.delivery.failed")).toBeNull();
    expect(telegramErrorKey(ERROR_CODES.telegramNotConfigured)).not.toBeNull();
  });
});

describe("link code expiry and countdown", () => {
  const now = Date.parse("2026-11-11T10:00:00Z");

  it("is active until the expiry instant, then expired", () => {
    expect(isLinkCodeActive("2026-11-11T10:10:00Z", now)).toBe(true);
    expect(isLinkCodeActive("2026-11-11T09:59:59Z", now)).toBe(false);
    expect(isLinkCodeActive(null, now)).toBe(false);
  });

  it("counts whole seconds left, never negative, and formats m:ss", () => {
    expect(secondsUntil("2026-11-11T10:10:00Z", now)).toBe(600);
    expect(secondsUntil("2026-11-11T09:00:00Z", now)).toBe(0);
    expect(formatCountdown(600)).toBe("10:00");
    expect(formatCountdown(65)).toBe("1:05");
    expect(formatCountdown(0)).toBe("0:00");
  });
});

describe("link status polling", () => {
  it("polls only while a code is pending and the account is not linked", () => {
    expect(shouldPollLink(true, false)).toBe(true);
    expect(shouldPollLink(true, undefined)).toBe(true);
    expect(shouldPollLink(true, true)).toBe(false);
    expect(shouldPollLink(false, false)).toBe(false);
  });

  it("uses the shared interval while polling and stops with false", () => {
    expect(linkPollInterval(true)).toBe(LIMITS.telegramLinkPollIntervalMs);
    expect(linkPollInterval(false)).toBe(false);
  });
});
