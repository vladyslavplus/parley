import { z } from "zod";

import { LIMITS } from "@/constants/limits";
import { HEX_COLOR_PATTERN } from "@/lib/stage-color";
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
    color: z.string().trim().regex(HEX_COLOR_PATTERN, t("hexColor")),
    probabilityBps: z.number().int().min(0).max(10000),
  });

export type PipelineStageValues = z.infer<
  ReturnType<typeof pipelineStageSchema>
>;
