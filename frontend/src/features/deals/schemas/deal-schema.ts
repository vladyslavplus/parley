import { z } from "zod";

import { LIMITS } from "@/constants/limits";
import { required, type ValidationT } from "@/lib/validation/fields";

export const createDealQuickSchema = (t: ValidationT) =>
  z.object({
    title: required(t, LIMITS.dealTitleMaxLength),
    amount: z.union([z.number(), z.string()]).refine((val) => {
      const num = typeof val === "number" ? val : parseFloat(val);
      return !isNaN(num) && num >= 0 && num <= LIMITS.dealAmountMax;
    }, t("invalidNumber")),
    currency: z
      .string()
      .trim()
      .length(
        LIMITS.currencyCodeLength,
        t("currency", { length: LIMITS.currencyCodeLength }),
      ),
    stageId: z.string().optional(),
    companyId: z.string().optional(),
    contactId: z.string().optional(),
    ownerUserId: z.string().optional(),
    expectedCloseOn: z.string().optional(),
  });

export type DealQuickValues = z.infer<ReturnType<typeof createDealQuickSchema>>;

export const dealLostSchema = (t: ValidationT) =>
  z.object({
    lostReason: z
      .string()
      .trim()
      .max(1000, t("tooLong", { max: 1000 }))
      .optional(),
  });

export type DealLostValues = z.infer<ReturnType<typeof dealLostSchema>>;
