import { z } from "zod";

import { LIMITS } from "@/constants/limits";
import { required, type ValidationT } from "@/lib/validation/fields";

export const createCompanyQuickSchema = (t: ValidationT) =>
  z.object({
    name: required(t, LIMITS.companyNameMaxLength),
    domain: z
      .string()
      .trim()
      .max(
        LIMITS.companyDomainMaxLength,
        t("tooLong", { max: LIMITS.companyDomainMaxLength }),
      )
      .optional(),
    country: z
      .string()
      .trim()
      .max(
        LIMITS.companyCountryMaxLength,
        t("tooLong", { max: LIMITS.companyCountryMaxLength }),
      )
      .optional(),
    industry: z
      .string()
      .trim()
      .max(
        LIMITS.companyIndustryMaxLength,
        t("tooLong", { max: LIMITS.companyIndustryMaxLength }),
      )
      .optional(),
  });

export type CompanyQuickValues = z.infer<
  ReturnType<typeof createCompanyQuickSchema>
>;
