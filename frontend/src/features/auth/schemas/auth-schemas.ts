import { z } from "zod";

import { LIMITS } from "@/constants/limits";
import {
  email,
  newPassword,
  required,
  type ValidationT,
} from "@/lib/validation/fields";

export const createLoginSchema = (t: ValidationT) =>
  z.object({
    email: email(t),
    password: z.string().min(1, t("required")),
  });
export type LoginValues = z.infer<ReturnType<typeof createLoginSchema>>;

export const createRegisterSchema = (t: ValidationT) =>
  z.object({
    organizationName: required(t, LIMITS.organizationNameMaxLength),
    email: email(t),
    password: newPassword(t),
  });
export type RegisterValues = z.infer<ReturnType<typeof createRegisterSchema>>;

export const createEmailSchema = (t: ValidationT) =>
  z.object({ email: email(t) });
export type EmailValues = z.infer<ReturnType<typeof createEmailSchema>>;

export const createResetPasswordSchema = (t: ValidationT) =>
  z
    .object({ newPassword: newPassword(t), confirmPassword: z.string() })
    .refine((v) => v.newPassword === v.confirmPassword, {
      path: ["confirmPassword"],
      message: t("passwordMismatch"),
    });
export type ResetPasswordValues = z.infer<
  ReturnType<typeof createResetPasswordSchema>
>;

export const createAcceptInvitationSchema = (t: ValidationT) =>
  z
    .object({ password: newPassword(t), confirmPassword: z.string() })
    .refine((v) => v.password === v.confirmPassword, {
      path: ["confirmPassword"],
      message: t("passwordMismatch"),
    });
export type AcceptInvitationValues = z.infer<
  ReturnType<typeof createAcceptInvitationSchema>
>;
