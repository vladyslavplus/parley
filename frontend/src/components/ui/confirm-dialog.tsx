"use client";

import { AlertTriangle, Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";
import { useRef, useState, type ReactNode } from "react";

import { describeError, toApiError } from "@/lib/errors";

import { Button } from "./button";
import { Modal } from "./modal";

export interface ConfirmDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
  onConfirm: () => void | Promise<void>;
}

/**
 * Non-dismissible confirmation: Escape and outside clicks do nothing, focus
 * starts on Cancel, and only the explicit buttons or the close cross end it.
 */
export function ConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel,
  cancelLabel,
  destructive = true,
  onConfirm,
}: ConfirmDialogProps) {
  const t = useTranslations("forms.alertDialog");
  const tKinds = useTranslations("errors.kinds");
  const [pending, setPending] = useState(false);
  const [confirmError, setConfirmError] = useState<string | null>(null);
  const [prevOpen, setPrevOpen] = useState(open);
  const cancelButtonRef = useRef<HTMLButtonElement>(null);

  if (open !== prevOpen) {
    setPrevOpen(open);
    setConfirmError(null);
  }

  function handleCancel() {
    if (pending) return;
    setConfirmError(null);
    onOpenChange(false);
  }

  async function handleConfirm() {
    if (pending) return;
    setPending(true);
    setConfirmError(null);
    try {
      await onConfirm();
      onOpenChange(false);
    } catch (err: unknown) {
      const apiError = toApiError(err);
      const msg = describeError(apiError, (kind) => tKinds(kind));
      setConfirmError(msg);
    } finally {
      setPending(false);
    }
  }

  return (
    <Modal
      open={open}
      onOpenChange={(nextOpen) => {
        if (!nextOpen) handleCancel();
      }}
      title={title}
      description={description}
      role="alertdialog"
      dismissible={false}
      closeDisabled={pending}
      bodyClassName="empty:hidden"
      icon={
        destructive ? (
          <div className="flex h-9 w-9 items-center justify-center rounded-full bg-destructive/10 text-destructive">
            <AlertTriangle className="h-5 w-5" aria-hidden="true" />
          </div>
        ) : undefined
      }
      onOpenAutoFocus={(event) => {
        event.preventDefault();
        cancelButtonRef.current?.focus();
      }}
      footer={
        <div className="flex justify-end gap-2">
          <Button
            ref={cancelButtonRef}
            type="button"
            variant="secondary"
            onClick={handleCancel}
            disabled={pending}
            className="px-3 text-xs"
          >
            {cancelLabel ?? t("cancel")}
          </Button>
          <Button
            type="button"
            variant={destructive ? "danger" : "primary"}
            onClick={() => void handleConfirm()}
            disabled={pending}
            className="gap-1.5 px-3 text-xs"
          >
            {pending && (
              <Loader2
                className="h-3.5 w-3.5 animate-spin"
                aria-hidden="true"
              />
            )}
            {confirmLabel ?? t("confirm")}
          </Button>
        </div>
      }
    >
      {confirmError ? (
        <div
          role="alert"
          className="rounded-[var(--radius-control)] border border-destructive/30 bg-destructive/10 p-2 text-xs text-destructive"
        >
          {confirmError}
        </div>
      ) : null}
    </Modal>
  );
}
