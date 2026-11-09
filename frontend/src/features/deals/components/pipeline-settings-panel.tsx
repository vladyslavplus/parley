"use client";

import * as Dialog from "@radix-ui/react-dialog";
import { ArrowDown, ArrowUp, Edit2, Lock, Plus, Trash2, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useId, useState } from "react";
import { useWatch } from "react-hook-form";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { useApiForm } from "@/components/forms/use-api-form";
import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { InlineEdit } from "@/components/ui/inline-edit";
import { Input } from "@/components/ui/input";
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
import { cn } from "@/lib/utils";

const STAGE_RAMP_CLASSES = [
  "bg-stage-1 border border-border/50",
  "bg-stage-2 border border-border/50",
  "bg-stage-3",
  "bg-stage-4",
  "bg-stage-5",
] as const;

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
            const colorIdx =
              typeof stage.colorIndex === "number"
                ? stage.colorIndex % STAGE_RAMP_CLASSES.length
                : 0;
            const dotClass =
              STAGE_RAMP_CLASSES[colorIdx] ?? STAGE_RAMP_CLASSES[0];
            const probPct = Math.round(
              (typeof stage.probabilityBps === "number"
                ? stage.probabilityBps
                : parseInt(String(stage.probabilityBps || "0"), 10)) / 100,
            );

            return (
              <div
                key={stage.id}
                className="flex items-center justify-between p-3 bg-card hover:bg-muted/30 transition-colors"
              >
                <div className="flex items-center gap-3">
                  <span
                    className={cn("h-3 w-3 shrink-0 rounded-full", dotClass)}
                    aria-hidden="true"
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
            {terminalStages.map((stage) => {
              const colorIdx =
                typeof stage.colorIndex === "number"
                  ? stage.colorIndex % STAGE_RAMP_CLASSES.length
                  : 0;
              const dotClass =
                STAGE_RAMP_CLASSES[colorIdx] ?? STAGE_RAMP_CLASSES[0];

              return (
                <div
                  key={stage.id}
                  className="flex items-center justify-between p-3"
                >
                  <div className="flex items-center gap-3">
                    <span
                      className={cn("h-3 w-3 shrink-0 rounded-full", dotClass)}
                      aria-hidden="true"
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
              );
            })}
          </div>
        </div>
      </div>

      {/* Add Stage Modal */}
      {addModalOpen && (
        <StageFormDialog
          open={addModalOpen}
          onOpenChange={setAddModalOpen}
          title={t("addStage")}
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
          initialValues={{
            name: editingStage.name,
            colorIndex: Number(editingStage.colorIndex),
            probabilityBps: Number(editingStage.probabilityBps),
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
  initialValues?: {
    name: string;
    colorIndex: number;
    probabilityBps: number;
  };
  onSubmitStage: (values: {
    name: string;
    colorIndex: number;
    probabilityBps: number;
  }) => Promise<void>;
}

function StageFormDialog({
  open,
  onOpenChange,
  title,
  initialValues,
  onSubmitStage,
}: StageFormDialogProps) {
  const t = useTranslations("settings.pipeline");
  const tCommon = useTranslations("common");
  const tValidation = useTranslations("validation");
  const schema = pipelineStageSchema((k, v) => tValidation(k, v));
  const colorLabelId = useId();
  const probabilityInputId = useId();

  const { form, onSubmit, formError, alertRef } =
    useApiForm<PipelineStageValues>({
      schema,
      defaultValues: {
        name: initialValues?.name ?? "",
        colorIndex: initialValues?.colorIndex ?? 0,
        probabilityBps: initialValues?.probabilityBps ?? 5000,
      },
      fields: ["name", "colorIndex", "probabilityBps"],
      submit: async (values) => {
        await onSubmitStage({
          name: values.name,
          colorIndex: values.colorIndex,
          probabilityBps: values.probabilityBps,
        });
      },
    });

  const {
    register,
    formState: { errors, isSubmitting, isDirty },
  } = form;

  const colorIndex =
    useWatch({
      control: form.control,
      name: "colorIndex",
    }) ?? 0;
  const probabilityBps =
    useWatch({
      control: form.control,
      name: "probabilityBps",
    }) ?? 5000;
  const probPct = Math.round(probabilityBps / 100);

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-background/80 backdrop-blur-xs" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-[var(--shadow-floating)] focus:outline-none space-y-4">
          <div className="flex items-center justify-between">
            <Dialog.Title className="text-base font-semibold text-foreground">
              {title}
            </Dialog.Title>
            <Dialog.Close asChild>
              <button
                type="button"
                aria-label={tCommon("close")}
                className="rounded-xs p-1 text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              >
                <X className="h-4 w-4" />
              </button>
            </Dialog.Close>
          </div>

          <ApiForm
            onSubmit={onSubmit}
            formError={formError}
            alertRef={alertRef}
            pending={isSubmitting}
            submitLabel={t("saveStage")}
            submitDisabled={Boolean(initialValues && !isDirty)}
          >
            <FormField
              label={t("stageName")}
              error={errors.name?.message}
              required
            >
              <Input
                {...register("name")}
                placeholder={t("stageNamePlaceholder")}
                maxLength={LIMITS.stageNameMaxLength}
                autoFocus
              />
            </FormField>

            {/* Color ramp picker */}
            <div className="space-y-1.5">
              <label
                id={colorLabelId}
                className="text-xs font-medium text-foreground block"
              >
                {t("stageColor")}
              </label>
              <div
                role="radiogroup"
                aria-labelledby={colorLabelId}
                className="flex items-center gap-2"
              >
                {STAGE_RAMP_CLASSES.map((dotClass, idx) => (
                  <button
                    key={idx}
                    type="button"
                    role="radio"
                    aria-checked={colorIndex === idx}
                    aria-label={`${t("stageColor")} ${idx + 1}`}
                    onClick={() => {
                      form.setValue("colorIndex", idx, {
                        shouldDirty: true,
                        shouldValidate: true,
                      });
                    }}
                    className={cn(
                      "h-7 w-7 rounded-full flex items-center justify-center transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
                      dotClass,
                      colorIndex === idx &&
                        "ring-2 ring-ring ring-offset-2 ring-offset-card scale-110",
                    )}
                  />
                ))}
              </div>
            </div>

            {/* Probability Slider / Input */}
            <div className="space-y-1.5">
              <div className="flex items-center justify-between text-xs">
                <label
                  htmlFor={probabilityInputId}
                  className="font-medium text-foreground"
                >
                  {t("probability")}
                </label>
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
                onChange={(e) => {
                  const val = parseInt(e.target.value, 10);
                  form.setValue("probabilityBps", val * 100, {
                    shouldDirty: true,
                    shouldValidate: true,
                  });
                }}
                className="w-full accent-ring"
              />
            </div>
          </ApiForm>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
