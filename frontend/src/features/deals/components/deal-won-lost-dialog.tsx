"use client";

import { AlertCircle, CheckCircle2, Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";
import { useId, useState } from "react";

import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/input";
import { Modal } from "@/components/ui/modal";
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
    <Modal
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen) handleCancel();
      }}
      title={isLost ? t("lostTitle") : t("wonTitle")}
      description={
        isLost
          ? t("lostDescription", { title: deal.title })
          : t("wonDescription", { title: deal.title })
      }
      closeDisabled={submitting}
      bodyClassName="empty:hidden"
      icon={
        <div
          className={`flex h-10 w-10 items-center justify-center rounded-[var(--radius-control)] ${
            isLost
              ? "bg-destructive/10 text-destructive"
              : "bg-success/10 text-success"
          }`}
        >
          {isLost ? (
            <AlertCircle className="h-5 w-5" aria-hidden="true" />
          ) : (
            <CheckCircle2 className="h-5 w-5" aria-hidden="true" />
          )}
        </div>
      }
      footer={
        <div className="flex justify-end gap-2">
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
            onClick={() => void handleConfirm()}
            disabled={submitting}
            className="px-3 text-xs"
          >
            {submitting && (
              <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
            )}
            {isLost ? t("confirmLost") : t("confirmWon")}
          </Button>
        </div>
      }
    >
      {isLost && (
        <div className="space-y-2">
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
    </Modal>
  );
}
