import { z } from "zod";

import { LIMITS } from "@/constants/limits";

export const noteFormSchema = z.object({
  body: z
    .string()
    .trim()
    .min(1, "bodyRequired")
    .max(LIMITS.noteBodyMaxLength, "bodyTooLong"),
  contactId: z.string().optional().nullable(),
  dealId: z.string().optional().nullable(),
  companyId: z.string().optional().nullable(),
});

export type NoteFormValues = z.infer<typeof noteFormSchema>;
