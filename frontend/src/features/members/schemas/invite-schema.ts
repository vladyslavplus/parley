import { z } from "zod";

import { email, type ValidationT } from "@/lib/validation/fields";

export const createInviteSchema = (t: ValidationT) =>
  z.object({
    email: email(t),
    roleCode: z.string().min(1, t("required")),
    teamId: z.string(),
  });
export type InviteValues = z.infer<ReturnType<typeof createInviteSchema>>;
