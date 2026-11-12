import { z } from "zod";

import { LIMITS } from "@/constants/limits";
import { TEMPLATE_LOCALES } from "@/features/campaigns/types";
import { required, type ValidationT } from "@/lib/validation/fields";

export const templateFormSchema = (t: ValidationT) =>
  z.object({
    name: required(t, LIMITS.templateNameMaxLength),
    locale: z.enum(TEMPLATE_LOCALES),
    subjectLiquid: required(t, LIMITS.templateSubjectMaxLength),
    bodyLiquid: required(t, LIMITS.templateBodyMaxLength),
  });

export type TemplateFormValues = z.infer<ReturnType<typeof templateFormSchema>>;
