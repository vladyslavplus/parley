import { z } from "zod";

import { LIMITS } from "@/constants/limits";
import { required, type ValidationT } from "@/lib/validation/fields";

export const createContactQuickSchema = (t: ValidationT) =>
  z.object({
    firstName: required(t, LIMITS.contactFirstNameMaxLength),
    lastName: z
      .string()
      .trim()
      .max(
        LIMITS.contactLastNameMaxLength,
        t("tooLong", { max: LIMITS.contactLastNameMaxLength }),
      )
      .optional(),
    email: z
      .string()
      .trim()
      .max(LIMITS.emailMaxLength, t("tooLong", { max: LIMITS.emailMaxLength }))
      .refine(
        (val) => !val || /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val),
        t("email"),
      )
      .optional(),
    phone: z
      .string()
      .trim()
      .max(
        LIMITS.contactPhoneMaxLength,
        t("tooLong", { max: LIMITS.contactPhoneMaxLength }),
      )
      .optional(),
    companyId: z.string().optional(),
  });

export type ContactQuickValues = z.infer<
  ReturnType<typeof createContactQuickSchema>
>;
