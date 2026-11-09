"use client";

import * as Dialog from "@radix-ui/react-dialog";
import { AlertTriangle, Loader2 } from "lucide-react";
import { useTranslations } from "next-intl";
import { useRef, useState, type ReactNode } from "react";

import { describeError, toApiError } from "@/lib/errors";
import { Button } from "./button";

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
    <Dialog.Root
      open={open}
      onOpenChange={(nextOpen) => {
        // Non-dismissible: ignore implicit close events from Radix Root;
        // only explicit Cancel or successful Confirm closes the dialog.
        if (nextOpen) {
          onOpenChange(true);
        }
      }}
    >
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/40 backdrop-blur-sm" />
        <Dialog.Content
          role="alertdialog"
          onEscapeKeyDown={(e) => e.preventDefault()}
          onPointerDownOutside={(e) => e.preventDefault()}
          onInteractOutside={(e) => e.preventDefault()}
          onOpenAutoFocus={(e) => {
            e.preventDefault();
            cancelButtonRef.current?.focus();
          }}
          className="fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-floating space-y-4 focus:outline-none"
        >
          <div className="flex items-start gap-3">
            {destructive && (
              <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-destructive/10 text-destructive mt-0.5">
                <AlertTriangle className="h-5 w-5" aria-hidden="true" />
              </div>
            )}
            <div className="space-y-1 min-w-0">
              <Dialog.Title className="text-sm font-semibold text-foreground">
                {title}
              </Dialog.Title>
              <Dialog.Description className="text-xs text-muted-foreground leading-relaxed">
                {description}
              </Dialog.Description>
            </div>
          </div>

          {confirmError ? (
            <div
              role="alert"
              className="rounded-[var(--radius-control)] border border-destructive/30 bg-destructive/10 p-2 text-xs text-destructive"
            >
              {confirmError}
            </div>
          ) : null}

          <div className="flex items-center justify-end gap-2 pt-2 border-t border-border">
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
              onClick={handleConfirm}
              disabled={pending}
              className="px-3 text-xs gap-1.5"
            >
              {pending && <Loader2 className="h-3.5 w-3.5 animate-spin" />}
              {confirmLabel ?? t("confirm")}
            </Button>
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
