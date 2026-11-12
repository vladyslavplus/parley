import { ERROR_CODES } from "@/constants/error-codes";
import { LIMITS } from "@/constants/limits";

// Telegram link vocabulary: which server codes have a friendly message, when the
// feature is shown at all, and when the link code or the link status is polled.
// Message keys are relative to the `notifications` namespace.

export const TELEGRAM_ERROR_KEYS = {
  [ERROR_CODES.telegramNotConfigured]: "telegram.errors.notConfigured",
  [ERROR_CODES.telegramLinkAlreadyExists]: "telegram.errors.linkAlreadyExists",
  [ERROR_CODES.telegramLinkCodeRateLimited]: "telegram.errors.rateLimited",
  [ERROR_CODES.telegramLinkCodeInvalid]: "telegram.errors.codeInvalid",
  [ERROR_CODES.telegramChatAlreadyLinked]: "telegram.errors.chatAlreadyLinked",
  [ERROR_CODES.telegramLinkNotFound]: "telegram.errors.linkNotFound",
} as const;

export type TelegramErrorKey =
  (typeof TELEGRAM_ERROR_KEYS)[keyof typeof TELEGRAM_ERROR_KEYS];

export function telegramErrorKey(
  code: string | null | undefined,
): TelegramErrorKey | null {
  return code && Object.prototype.hasOwnProperty.call(TELEGRAM_ERROR_KEYS, code)
    ? TELEGRAM_ERROR_KEYS[code as keyof typeof TELEGRAM_ERROR_KEYS]
    : null;
}

// Telegram is shown only when the server reports it enabled. With no bot token
// configured the server answers `enabled: false`, and nothing about Telegram is
// rendered: no buttons, no column, no mention.
export function telegramVisible(
  status: { enabled: boolean } | undefined,
): boolean {
  return status?.enabled === true;
}

// A code is usable until its expiry instant passes.
export function isLinkCodeActive(
  expiresAt: string | null,
  nowMs: number,
): boolean {
  return expiresAt !== null && Date.parse(expiresAt) > nowMs;
}

// Whole seconds left on a code, never negative.
export function secondsUntil(expiresAt: string, nowMs: number): number {
  return Math.max(0, Math.ceil((Date.parse(expiresAt) - nowMs) / 1000));
}

// Poll the link only while a code is pending and the account is not linked yet:
// once linked, the poll stops; without a pending code there is nothing to wait for.
export function shouldPollLink(
  codePending: boolean,
  linked: boolean | undefined,
): boolean {
  return codePending && linked !== true;
}

export function linkPollInterval(poll: boolean): number | false {
  return poll ? LIMITS.telegramLinkPollIntervalMs : false;
}

// Formats seconds as m:ss for the countdown.
export function formatCountdown(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}
