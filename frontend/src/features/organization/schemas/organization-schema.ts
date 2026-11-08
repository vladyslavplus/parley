import { z } from "zod";

import { LIMITS } from "@/constants/limits";
import { required, type ValidationT } from "@/lib/validation/fields";

export const createOrganizationSchema = (t: ValidationT) =>
  z.object({
    name: required(t, LIMITS.organizationNameMaxLength),
    timeZone: z.string().min(1, t("required")),
    defaultLocale: z.string().min(1, t("required")),
    currency: z
      .string()
      .trim()
      .length(
        LIMITS.currencyCodeLength,
        t("currency", { length: LIMITS.currencyCodeLength }),
      ),
  });
export type OrganizationValues = z.infer<
  ReturnType<typeof createOrganizationSchema>
>;
