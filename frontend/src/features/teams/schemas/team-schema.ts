import { z } from "zod";

import { LIMITS } from "@/constants/limits";
import { required, type ValidationT } from "@/lib/validation/fields";

export const createTeamSchema = (t: ValidationT) =>
  z.object({ name: required(t, LIMITS.teamNameMaxLength) });
export type TeamValues = z.infer<ReturnType<typeof createTeamSchema>>;
