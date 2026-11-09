import { z } from "zod";

import { CUSTOM_FIELD_KEY_REGEX, LIMITS } from "@/constants/limits";
import { required, type ValidationT } from "@/lib/validation/fields";

export const createCustomFieldSchema = (t: ValidationT) =>
  z.object({
    key: z
      .string()
      .trim()
      .min(
        LIMITS.customFieldKeyMinLength,
        t("tooShort", { min: LIMITS.customFieldKeyMinLength }),
      )
      .max(
        LIMITS.customFieldKeyMaxLength,
        t("tooLong", { max: LIMITS.customFieldKeyMaxLength }),
      )
      .regex(CUSTOM_FIELD_KEY_REGEX, t("customFieldKey")),
    label: required(t, LIMITS.customFieldLabelMaxLength),
    entityType: z.enum(["contact", "company"]),
    fieldType: z.enum(["text", "number", "boolean", "select", "date"]),
    options: z.string().optional(),
    isRequired: z.boolean(),
    position: z
      .number(t("invalidNumber"))
      .int(t("invalidNumber"))
      .min(0, t("invalidNumber")),
  });

export type CreateCustomFieldValues = z.infer<
  ReturnType<typeof createCustomFieldSchema>
>;

export const updateCustomFieldSchema = (t: ValidationT) =>
  z.object({
    label: required(t, LIMITS.customFieldLabelMaxLength),
    options: z.string().optional(),
    isRequired: z.boolean(),
    position: z
      .number(t("invalidNumber"))
      .int(t("invalidNumber"))
      .min(0, t("invalidNumber")),
  });

export type UpdateCustomFieldValues = z.infer<
  ReturnType<typeof updateCustomFieldSchema>
>;
