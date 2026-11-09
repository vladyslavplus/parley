"use client";

import * as Dialog from "@radix-ui/react-dialog";
import { AlertCircle, CheckCircle2, Loader2, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useId, useState } from "react";

import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/input";
import type {
  Deal,
  PipelineBoardStage,
  PipelineStage,
} from "@/features/deals/types";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  deal: Deal | null;
  targetStage: PipelineBoardStage | PipelineStage | null;
  position: number;
  onConfirm: (payload: {
    dealId: string;
    stageId: string;
    position: number;
    version: number | string;
    lostReason?: string | null;
  }) => Promise<void>;
}

export function DealWonLostDialog({
  open,
  onOpenChange,
  deal,
  targetStage,
  position,
  onConfirm,
}: Props) {
  const t = useTranslations("crm.deals.terminalDialog");
  const tCommon = useTranslations("common");
  const descriptionId = useId();
  const reasonId = useId();

  const [lostReason, setLostReason] = useState("");
  const [submitting, setSubmitting] = useState(false);
  const [prevOpen, setPrevOpen] = useState(open);

  if (open !== prevOpen) {
    setPrevOpen(open);
    setLostReason("");
    setSubmitting(false);
  }

  if (!deal || !targetStage) return null;

  const isLost = targetStage.kind === "lost";

  async function handleConfirm() {
    if (!deal || !targetStage) return;
    setSubmitting(true);
    try {
      await onConfirm({
        dealId: deal.id,
        stageId: targetStage.id,
        position,
        version: deal.version,
        lostReason: isLost ? lostReason.trim() || null : null,
      });
      onOpenChange(false);
    } catch {
      // Error is handled by caller
    } finally {
      setSubmitting(false);
    }
  }

  function handleCancel() {
    if (submitting) return;
    onOpenChange(false);
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-background/80 backdrop-blur-xs transition-opacity" />
        <Dialog.Content
          aria-describedby={descriptionId}
          className="fixed left-[50%] top-[50%] z-50 w-full max-w-md translate-x-[-50%] translate-y-[-50%] rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-[var(--shadow-floating)] focus:outline-none"
        >
          <div className="flex items-start justify-between gap-4">
            <div className="flex items-center gap-3">
              <div
                className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-[var(--radius-control)] ${
                  isLost
                    ? "bg-destructive/10 text-destructive"
                    : "bg-success/10 text-success"
                }`}
              >
                {isLost ? (
                  <AlertCircle className="h-5 w-5" />
                ) : (
                  <CheckCircle2 className="h-5 w-5" />
                )}
              </div>
              <div>
                <Dialog.Title className="text-base font-semibold text-foreground">
                  {isLost ? t("lostTitle") : t("wonTitle")}
                </Dialog.Title>
                <Dialog.Description
                  id={descriptionId}
                  className="text-xs text-muted-foreground mt-0.5"
                >
                  {isLost
                    ? t("lostDescription", { title: deal.title })
                    : t("wonDescription", { title: deal.title })}
                </Dialog.Description>
              </div>
            </div>
            <Dialog.Close asChild>
              <button
                type="button"
                onClick={handleCancel}
                disabled={submitting}
                className="rounded-xs p-1 text-muted-foreground hover:bg-muted hover:text-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              >
                <X className="h-4 w-4" />
                <span className="sr-only">{tCommon("close")}</span>
              </button>
            </Dialog.Close>
          </div>

          {isLost && (
            <div className="mt-4 space-y-2">
              <label
                htmlFor={reasonId}
                className="block text-xs font-medium text-foreground"
              >
                {t("lostReasonLabel")}
              </label>
              <Textarea
                id={reasonId}
                value={lostReason}
                onChange={(e) => setLostReason(e.target.value)}
                rows={3}
                placeholder={t("lostReasonPlaceholder")}
                disabled={submitting}
                autoFocus
              />
            </div>
          )}

          <div className="mt-6 flex justify-end gap-2">
            <Button
              type="button"
              variant="secondary"
              onClick={handleCancel}
              disabled={submitting}
              className="px-3 text-xs"
            >
              {t("cancel")}
            </Button>
            <Button
              type="button"
              variant={isLost ? "danger" : "primary"}
              onClick={handleConfirm}
              disabled={submitting}
              className="px-3 text-xs"
            >
              {submitting && (
                <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
              )}
              {isLost ? t("confirmLost") : t("confirmWon")}
            </Button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
