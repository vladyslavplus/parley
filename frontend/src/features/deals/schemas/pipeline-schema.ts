import { z } from "zod";

import { LIMITS } from "@/constants/limits";
import { required, type ValidationT } from "@/lib/validation/fields";

export const renamePipelineSchema = (t: ValidationT) =>
  z.object({
    name: required(t, LIMITS.pipelineNameMaxLength),
  });

export type RenamePipelineValues = z.infer<
  ReturnType<typeof renamePipelineSchema>
>;

export const pipelineStageSchema = (t: ValidationT) =>
  z.object({
    name: required(t, LIMITS.stageNameMaxLength),
    colorIndex: z
      .number()
      .int()
      .min(0)
      .max(LIMITS.stageColorCount - 1),
    probabilityBps: z.number().int().min(0).max(10000),
  });

export type PipelineStageValues = z.infer<
  ReturnType<typeof pipelineStageSchema>
>;
