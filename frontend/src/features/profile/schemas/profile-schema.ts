import { z } from "zod";

import { LIMITS } from "@/constants/limits";
import type { ValidationT } from "@/lib/validation/fields";

export const createProfileSchema = (t: ValidationT) =>
  z.object({
    displayName: z
      .string()
      .trim()
      .max(
        LIMITS.displayNameMaxLength,
        t("tooLong", { max: LIMITS.displayNameMaxLength }),
      ),
    locale: z.string().min(1, t("required")),
    timeZone: z.string().min(1, t("required")),
    theme: z.string().min(1, t("required")),
  });
export type ProfileValues = z.infer<ReturnType<typeof createProfileSchema>>;
