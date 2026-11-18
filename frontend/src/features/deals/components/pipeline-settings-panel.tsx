"use client";

import {
  ArrowDown,
  ArrowUp,
  Edit2,
  Loader2,
  Lock,
  Plus,
  Trash2,
} from "lucide-react";
import { useTranslations } from "next-intl";
import { useId, useState } from "react";
import { useWatch } from "react-hook-form";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { useApiForm } from "@/components/forms/use-api-form";
import { Button } from "@/components/ui/button";
import { ColorPicker } from "@/components/ui/color-picker";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { InfoHint } from "@/components/ui/info-hint";
import { InlineEdit } from "@/components/ui/inline-edit";
import { Input } from "@/components/ui/input";
import { Modal, ModalFooter } from "@/components/ui/modal";
import { Skeleton } from "@/components/ui/skeleton";
import { LIMITS } from "@/constants/limits";
import { PERMISSIONS } from "@/constants/permissions";
import { usePipelineMutations } from "@/features/deals/hooks/use-pipeline-mutations";
import { useDefaultPipeline } from "@/features/deals/hooks/use-pipelines";
import {
  pipelineStageSchema,
  type PipelineStageValues,
} from "@/features/deals/schemas/pipeline-schema";
import type { PipelineStage } from "@/features/deals/types";
import { useCan } from "@/features/session/components/can";
import { stageColorStyle } from "@/lib/stage-color";
import { STAGE_DEFAULT_COLORS } from "@/lib/tokens";

const STAGE_FORM_ID = "pipeline-stage-form";

function asNumber(value: number | string): number {
  return typeof value === "number" ? value : parseInt(value || "0", 10);
}

export function PipelineSettingsPanel() {
  const t = useTranslations("settings.pipeline");
  const tCommon = useTranslations("common");
  const canManage = useCan(PERMISSIONS.pipelinesManage);

  const { pipeline, isLoading } = useDefaultPipeline();
  const {
    renamePipeline,
    createStage,
    updateStage,
    deleteStage,
    reorderStages,
  } = usePipelineMutations();

  const [addModalOpen, setAddModalOpen] = useState(false);
  const [editingStage, setEditingStage] = useState<PipelineStage | null>(null);
  const [deletingStage, setDeletingStage] = useState<PipelineStage | null>(
    null,
  );

  const stages = pipeline?.stages ?? [];
  const openStages = stages.filter((s) => s.kind === "open");
  const terminalStages = stages.filter((s) => s.kind !== "open");
  const nextDefaultColor =
    STAGE_DEFAULT_COLORS[openStages.length % STAGE_DEFAULT_COLORS.length];

  async function handleRename(newName: string) {
    if (!pipeline) return;
    await renamePipeline(pipeline.id, newName, pipeline.version);
  }

  async function handleMoveStage(index: number, direction: "up" | "down") {
    if (!pipeline || !canManage) return;
    const targetIndex = direction === "up" ? index - 1 : index + 1;
    if (targetIndex < 0 || targetIndex >= openStages.length) return;

    const reordered = [...openStages];
    const temp = reordered[index]!;
    reordered[index] = reordered[targetIndex]!;
    reordered[targetIndex] = temp;

    const newStageIds = reordered.map((s) => s.id);
    await reorderStages(pipeline.id, newStageIds);
  }

  async function handleDeleteStageConfirm() {
    if (!pipeline || !deletingStage) return;
    await deleteStage(pipeline.id, deletingStage.id);
    setDeletingStage(null);
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div>
          <Skeleton className="h-8 w-48" />
          <Skeleton className="mt-1 h-4 w-96" />
        </div>
        <div className="rounded-[var(--radius-card)] border border-border bg-card p-6 space-y-4">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
        </div>
      </div>
    );
  }

  if (!pipeline) {
    return (
      <div className="rounded-[var(--radius-card)] border border-border bg-card p-8 text-center text-sm text-muted-foreground">
        {t("noPipeline")}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header Actions */}
      <div className="flex flex-wrap items-center justify-between gap-4">
        <p className="text-xs text-muted-foreground">{t("subtitle")}</p>

        {canManage && (
          <Button
            onClick={() => setAddModalOpen(true)}
            className="gap-1.5 px-2.5 text-xs"
          >
            <Plus className="h-3.5 w-3.5" />
            <span>{t("addStage")}</span>
          </Button>
        )}
      </div>

      {/* Pipeline name card */}
      <div className="rounded-[var(--radius-card)] border border-border bg-card p-5 shadow-xs">
        <label className="text-xs font-medium text-muted-foreground block mb-1.5">
          {t("pipelineName")}
        </label>
        <InlineEdit
          value={pipeline.name}
          onSave={handleRename}
          disabled={!canManage}
          className="text-base font-semibold text-foreground"
        />
      </div>

      {/* Stages list container */}
      <div className="rounded-[var(--radius-card)] border border-border bg-card p-5 shadow-xs space-y-4">
        <div>
          <h2 className="text-sm font-semibold text-foreground">
            {t("stagesTitle")}
          </h2>
          <p className="text-xs text-muted-foreground mt-0.5">
            {t("stagesDescription")}
          </p>
        </div>

        {/* Open stages */}
        <div className="divide-y divide-border rounded-[var(--radius-control)] border border-border overflow-hidden">
          {openStages.map((stage, idx) => {
            const probPct = Math.round(asNumber(stage.probabilityBps) / 100);

            return (
              <div
                key={stage.id}
                className="flex items-center justify-between p-3 bg-card hover:bg-muted/30 transition-colors"
              >
                <div className="flex items-center gap-3">
                  <span
                    aria-hidden="true"
                    className="h-3 w-3 shrink-0 rounded-full border border-border/60"
                    style={stageColorStyle(stage.color)}
                  />
                  <div>
                    <div className="text-xs font-medium text-foreground">
                      {stage.name}
                    </div>
                    <div className="text-[11px] text-muted-foreground">
                      {t("probability")}: {probPct}%
                    </div>
                  </div>
                </div>

                {canManage && (
                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      onClick={() => void handleMoveStage(idx, "up")}
                      disabled={idx === 0}
                      aria-label={t("moveUp")}
                      className="h-7 w-7 rounded-[var(--radius-control)] p-0 flex items-center justify-center text-muted-foreground hover:bg-muted hover:text-foreground transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                      <ArrowUp className="h-3.5 w-3.5" />
                    </button>
                    <button
                      type="button"
                      onClick={() => void handleMoveStage(idx, "down")}
                      disabled={idx === openStages.length - 1}
                      aria-label={t("moveDown")}
                      className="h-7 w-7 rounded-[var(--radius-control)] p-0 flex items-center justify-center text-muted-foreground hover:bg-muted hover:text-foreground transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                      <ArrowDown className="h-3.5 w-3.5" />
                    </button>
                    <button
                      type="button"
                      onClick={() => setEditingStage(stage)}
                      aria-label={t("editStage")}
                      className="h-7 w-7 rounded-[var(--radius-control)] p-0 flex items-center justify-center text-muted-foreground hover:bg-muted hover:text-foreground transition-colors"
                    >
                      <Edit2 className="h-3.5 w-3.5" />
                    </button>
                    <button
                      type="button"
                      onClick={() => setDeletingStage(stage)}
                      disabled={openStages.length <= 1}
                      aria-label={t("deleteStage")}
                      className="h-7 w-7 rounded-[var(--radius-control)] p-0 flex items-center justify-center text-destructive hover:bg-destructive/10 hover:text-destructive transition-colors disabled:opacity-40 disabled:cursor-not-allowed"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </div>
                )}
              </div>
            );
          })}
        </div>

        {/* Terminal stages (Won & Lost - fixed) */}
        <div className="pt-2">
          <div className="text-xs font-medium text-muted-foreground mb-2 flex items-center gap-1.5">
            <Lock className="h-3 w-3" />
            <span>{t("terminalStagesTitle")}</span>
          </div>
          <div className="divide-y divide-border rounded-[var(--radius-control)] border border-border bg-muted/20 overflow-hidden">
            {terminalStages.map((stage) => (
              <div
                key={stage.id}
                className="flex items-center justify-between p-3"
              >
                <div className="flex items-center gap-3">
                  <span
                    aria-hidden="true"
                    className="h-3 w-3 shrink-0 rounded-full border border-border/60"
                    style={stageColorStyle(stage.color)}
                  />
                  <div>
                    <div className="text-xs font-medium text-foreground">
                      {stage.name}
                    </div>
                    <div className="text-[10px] text-muted-foreground uppercase tracking-wider">
                      {stage.kind === "won" ? t("stageWon") : t("stageLost")}
                    </div>
                  </div>
                </div>

                {canManage && (
                  <button
                    type="button"
                    onClick={() => setEditingStage(stage)}
                    aria-label={t("editStage")}
                    className="h-7 w-7 rounded-[var(--radius-control)] p-0 flex items-center justify-center text-muted-foreground hover:bg-muted hover:text-foreground transition-colors"
                  >
                    <Edit2 className="h-3.5 w-3.5" />
                  </button>
                )}
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Add Stage Modal */}
      {addModalOpen && (
        <StageFormDialog
          open={addModalOpen}
          onOpenChange={setAddModalOpen}
          title={t("addStage")}
          defaultColor={nextDefaultColor}
          onSubmitStage={async (values) => {
            if (!pipeline) return;
            await createStage(pipeline.id, values);
            setAddModalOpen(false);
          }}
        />
      )}

      {/* Edit Stage Modal */}
      {editingStage && (
        <StageFormDialog
          open={Boolean(editingStage)}
          onOpenChange={(open) => {
            if (!open) setEditingStage(null);
          }}
          title={t("editStage")}
          defaultColor={nextDefaultColor}
          initialValues={{
            name: editingStage.name,
            color: editingStage.color,
            probabilityBps: asNumber(editingStage.probabilityBps),
          }}
          onSubmitStage={async (values) => {
            if (!pipeline || !editingStage) return;
            await updateStage(pipeline.id, editingStage.id, values);
            setEditingStage(null);
          }}
        />
      )}

      {/* Delete Stage Confirm */}
      <ConfirmDialog
        open={Boolean(deletingStage)}
        onOpenChange={(open) => {
          if (!open) setDeletingStage(null);
        }}
        title={t("deleteStageTitle")}
        description={t("deleteStageDescription", {
          name: deletingStage?.name ?? "",
        })}
        confirmLabel={t("deleteStage")}
        cancelLabel={tCommon("cancel")}
        onConfirm={handleDeleteStageConfirm}
      />
    </div>
  );
}

interface StageFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  /** Colour a new stage starts on: the next default ramp colour. */
  defaultColor: string;
  initialValues?: {
    name: string;
    color: string;
    probabilityBps: number;
  };
  onSubmitStage: (values: {
    name: string;
    color: string;
    probabilityBps: number;
  }) => Promise<void>;
}

function StageFormDialog({
  open,
  onOpenChange,
  title,
  defaultColor,
  initialValues,
  onSubmitStage,
}: StageFormDialogProps) {
  const t = useTranslations("settings.pipeline");
  const tHints = useTranslations("hints");
  const tValidation = useTranslations("validation");
  const schema = pipelineStageSchema((k, v) => tValidation(k, v));
  const probabilityInputId = useId();
  const colorInputId = useId();
  const colorErrorId = useId();

  const { form, onSubmit, formError, alertRef } =
    useApiForm<PipelineStageValues>({
      schema,
      defaultValues: {
        name: initialValues?.name ?? "",
        color: initialValues?.color ?? defaultColor,
        probabilityBps: initialValues?.probabilityBps ?? 5000,
      },
      fields: ["name", "color", "probabilityBps"],
      submit: async (values) => {
        await onSubmitStage({
          name: values.name,
          color: values.color,
          probabilityBps: values.probabilityBps,
        });
      },
    });

  const {
    register,
    formState: { errors, isSubmitting, isDirty },
  } = form;

  const color =
    useWatch({
      control: form.control,
      name: "color",
    }) ?? defaultColor;
  const probabilityBps =
    useWatch({
      control: form.control,
      name: "probabilityBps",
    }) ?? 5000;
  const probPct = Math.round(probabilityBps / 100);

  return (
    <Modal open={open} onOpenChange={onOpenChange} title={title}>
      <ApiForm
        id={STAGE_FORM_ID}
        onSubmit={onSubmit}
        formError={formError}
        alertRef={alertRef}
        pending={isSubmitting}
      >
        <FormField label={t("stageName")} error={errors.name?.message} required>
          <Input
            {...register("name")}
            placeholder={t("stageNamePlaceholder")}
            maxLength={LIMITS.stageNameMaxLength}
            autoFocus
          />
        </FormField>

        <div className="space-y-1.5">
          <div className="flex items-center gap-1">
            <label
              htmlFor={colorInputId}
              className="text-xs font-medium text-foreground"
            >
              {t("stageColor")}
            </label>
            <InfoHint>{tHints("pipelineStageColor")}</InfoHint>
          </div>
          <ColorPicker
            id={colorInputId}
            value={color}
            invalid={Boolean(errors.color)}
            aria-describedby={errors.color ? colorErrorId : undefined}
            onChange={(next) => {
              form.setValue("color", next, {
                shouldDirty: true,
                shouldValidate: true,
              });
            }}
          />
          {errors.color ? (
            <p
              id={colorErrorId}
              className="text-xs font-medium text-destructive"
            >
              {errors.color.message}
            </p>
          ) : null}
        </div>

        <div className="space-y-1.5">
          <div className="flex items-center justify-between text-xs">
            <span className="flex items-center gap-1">
              <label
                htmlFor={probabilityInputId}
                className="font-medium text-foreground"
              >
                {t("probability")}
              </label>
              <InfoHint>{tHints("pipelineProbability")}</InfoHint>
            </span>
            <span className="font-semibold text-muted-foreground tabular-nums">
              {probPct}%
            </span>
          </div>
          <input
            id={probabilityInputId}
            type="range"
            min="0"
            max="100"
            value={probPct}
            onChange={(event) => {
              const val = parseInt(event.target.value, 10);
              form.setValue("probabilityBps", val * 100, {
                shouldDirty: true,
                shouldValidate: true,
              });
            }}
            className="w-full accent-ring"
          />
        </div>

        <ModalFooter>
          <Button
            type="submit"
            form={STAGE_FORM_ID}
            className="w-full"
            disabled={isSubmitting || Boolean(initialValues && !isDirty)}
          >
            {isSubmitting ? (
              <Loader2 className="h-3.5 w-3.5 animate-spin" />
            ) : null}
            {t("saveStage")}
          </Button>
        </ModalFooter>
      </ApiForm>
    </Modal>
  );
}
