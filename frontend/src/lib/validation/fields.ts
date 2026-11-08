import { z } from "zod";

import { LIMITS } from "@/constants/limits";
import type { Messages } from "@/i18n/messages";

export type ValidationKey = keyof Messages["validation"];
export type ValidationT = (
  key: ValidationKey,
  values?: Record<string, string | number>,
) => string;

export const email = (t: ValidationT) =>
  z
    .string()
    .trim()
    .min(1, t("required"))
    .max(LIMITS.emailMaxLength, t("tooLong", { max: LIMITS.emailMaxLength }))
    .pipe(z.email(t("email")));

export const newPassword = (t: ValidationT) =>
  z
    .string()
    .min(
      LIMITS.passwordMinLength,
      t("passwordMin", { min: LIMITS.passwordMinLength }),
    )
    .max(
      LIMITS.passwordMaxLength,
      t("tooLong", { max: LIMITS.passwordMaxLength }),
    );

export const required = (t: ValidationT, max: number) =>
  z.string().trim().min(1, t("required")).max(max, t("tooLong", { max }));
