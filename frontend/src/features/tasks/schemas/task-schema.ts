import { z } from "zod";

import { LIMITS } from "@/constants/limits";

export const taskFormSchema = z.object({
  title: z
    .string()
    .trim()
    .min(1, "titleRequired")
    .max(LIMITS.taskTitleMaxLength, "titleTooLong"),
  notes: z
    .string()
    .trim()
    .max(LIMITS.taskNotesMaxLength, "notesTooLong")
    .optional()
    .nullable(),
  dueAt: z.string().optional().nullable(),
  assigneeUserId: z.string().min(1, "assigneeRequired"),
  contactId: z.string().optional().nullable(),
  dealId: z.string().optional().nullable(),
  companyId: z.string().optional().nullable(),
});

export type TaskFormValues = z.infer<typeof taskFormSchema>;
