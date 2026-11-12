import { z } from "zod";

import { LIMITS } from "@/constants/limits";
import { required, type ValidationT } from "@/lib/validation/fields";

export const segmentFormSchema = (t: ValidationT) =>
  z.object({
    name: required(t, LIMITS.segmentNameMaxLength),
    ownerUserIds: z.array(z.string()),
    countryCodes: z.array(z.string()),
    tagIds: z.array(z.string()),
    daysSinceLastOrder: z
      .string()
      .trim()
      .refine(
        (value) =>
          value === "" ||
          (/^\d+$/.test(value) &&
            Number(value) >= 1 &&
            Number(value) <= LIMITS.segmentDaysSinceLastOrderMax),
        t("invalidNumber"),
      ),
  });

export type SegmentFormValues = z.infer<ReturnType<typeof segmentFormSchema>>;
