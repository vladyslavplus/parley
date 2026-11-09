"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";

import { ERROR_CODES } from "@/constants/error-codes";
import { queryKeys } from "@/constants/query-keys";
import type { Pipeline, PipelineStage } from "@/features/deals/types";
import { backendJson, backendVoid } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";
import { describeError, toApiError } from "@/lib/errors";
import { showErrorToast, showSuccessToast } from "@/lib/toasts";

export interface StageInput {
  name: string;
  colorIndex: number;
  probabilityBps: number;
}

export function usePipelineMutations() {
  const locale = useLocale();
  const queryClient = useQueryClient();
  const tCrm = useTranslations("crm");
  const tErrors = useTranslations("errors.kinds");
  const tSettings = useTranslations("settings.pipeline");

  function handleConflict(pipelineId: string) {
    showErrorToast(tCrm("conflict.title"), {
      description: tCrm("conflict.description"),
      action: {
        label: tCrm("conflict.reload"),
        onClick: () => {
          void queryClient.invalidateQueries({
            queryKey: queryKeys.pipeline(pipelineId),
          });
        },
      },
    });
    void queryClient.invalidateQueries({
      queryKey: queryKeys.pipeline(pipelineId),
    });
  }

  async function renamePipeline(
    id: string,
    name: string,
    version: number | string,
  ): Promise<Pipeline> {
    const body: Schemas["PipelineRenameRequest"] = {
      name,
      version,
    };

    try {
      const updated = await backendJson<Pipeline>(
        "/pipelines/{id}",
        {
          method: "PUT",
          body,
          locale,
        },
        `/pipelines/${encodeURIComponent(id)}`,
      );

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.pipeline(id) }),
        queryClient.invalidateQueries({ queryKey: queryKeys.pipelinesAll() }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.pipelineBoard(id),
        }),
      ]);

      showSuccessToast(tSettings("saved"));
      return updated;
    } catch (err) {
      const apiErr = toApiError(err);
      if (
        apiErr.status === 409 ||
        apiErr.code === ERROR_CODES.concurrencyConflict
      ) {
        handleConflict(id);
      } else {
        showErrorToast(
          describeError(apiErr, (kind) =>
            tErrors(kind as Parameters<typeof tErrors>[0]),
          ),
        );
      }
      throw apiErr;
    }
  }

  async function createStage(
    pipelineId: string,
    input: StageInput,
  ): Promise<PipelineStage> {
    const body: Schemas["PipelineStageRequest"] = {
      name: input.name,
      colorIndex: input.colorIndex,
      probabilityBps: input.probabilityBps,
    };

    try {
      const created = await backendJson<PipelineStage>(
        "/pipelines/{id}/stages",
        {
          method: "POST",
          body,
          locale,
        },
        `/pipelines/${encodeURIComponent(pipelineId)}/stages`,
      );

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: queryKeys.pipeline(pipelineId),
        }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.pipelineBoard(pipelineId),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.dealsAll() }),
      ]);

      showSuccessToast(tSettings("stageCreated"));
      return created;
    } catch (err) {
      const apiErr = toApiError(err);
      showErrorToast(
        describeError(apiErr, (kind) =>
          tErrors(kind as Parameters<typeof tErrors>[0]),
        ),
      );
      throw apiErr;
    }
  }

  async function updateStage(
    pipelineId: string,
    stageId: string,
    input: StageInput,
  ): Promise<PipelineStage> {
    const body: Schemas["PipelineStageRequest"] = {
      name: input.name,
      colorIndex: input.colorIndex,
      probabilityBps: input.probabilityBps,
    };

    try {
      const updated = await backendJson<PipelineStage>(
        "/pipelines/{id}/stages/{stageId}",
        {
          method: "PUT",
          body,
          locale,
        },
        `/pipelines/${encodeURIComponent(pipelineId)}/stages/${encodeURIComponent(stageId)}`,
      );

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: queryKeys.pipeline(pipelineId),
        }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.pipelineBoard(pipelineId),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.dealsAll() }),
      ]);

      showSuccessToast(tSettings("stageUpdated"));
      return updated;
    } catch (err) {
      const apiErr = toApiError(err);
      showErrorToast(
        describeError(apiErr, (kind) =>
          tErrors(kind as Parameters<typeof tErrors>[0]),
        ),
      );
      throw apiErr;
    }
  }

  async function deleteStage(
    pipelineId: string,
    stageId: string,
  ): Promise<void> {
    try {
      await backendVoid(
        "/pipelines/{id}/stages/{stageId}",
        {
          method: "DELETE",
          locale,
        },
        `/pipelines/${encodeURIComponent(pipelineId)}/stages/${encodeURIComponent(stageId)}`,
      );

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: queryKeys.pipeline(pipelineId),
        }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.pipelineBoard(pipelineId),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.dealsAll() }),
      ]);

      showSuccessToast(tSettings("stageDeleted"));
    } catch (err) {
      throw toApiError(err);
    }
  }

  async function reorderStages(
    pipelineId: string,
    stageIds: string[],
  ): Promise<Pipeline> {
    const body: Schemas["PipelineStageOrderRequest"] = {
      stageIds,
    };

    try {
      const updated = await backendJson<Pipeline>(
        "/pipelines/{id}/stages/order",
        {
          method: "PUT",
          body,
          locale,
        },
        `/pipelines/${encodeURIComponent(pipelineId)}/stages/order`,
      );

      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: queryKeys.pipeline(pipelineId),
        }),
        queryClient.invalidateQueries({
          queryKey: queryKeys.pipelineBoard(pipelineId),
        }),
        queryClient.invalidateQueries({ queryKey: queryKeys.dealsAll() }),
      ]);

      showSuccessToast(tSettings("stagesReordered"));
      return updated;
    } catch (err) {
      const apiErr = toApiError(err);
      showErrorToast(
        describeError(apiErr, (kind) =>
          tErrors(kind as Parameters<typeof tErrors>[0]),
        ),
      );
      throw apiErr;
    }
  }

  return {
    renamePipeline,
    createStage,
    updateStage,
    deleteStage,
    reorderStages,
  };
}
