"use client";

import * as Dialog from "@radix-ui/react-dialog";
import { AlertTriangle, Loader2, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState, type ReactNode } from "react";

import { Button } from "./button";
import { describeError, toApiError } from "@/lib/errors";

export interface AlertDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  title: string;
  description: ReactNode;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
  onConfirm: () => void | Promise<void>;
}

export function AlertDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel,
  cancelLabel,
  destructive = true,
  onConfirm,
}: AlertDialogProps) {
  const t = useTranslations("forms.alertDialog");
  const tCommon = useTranslations("common");
  const tKinds = useTranslations("errors.kinds");
  const [pending, setPending] = useState(false);
  const [confirmError, setConfirmError] = useState<string | null>(null);

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
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/40 backdrop-blur-sm" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-floating space-y-4">
          <div className="flex items-start justify-between">
            <div className="flex items-start gap-3">
              {destructive && (
                <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-destructive/10 text-destructive mt-0.5">
                  <AlertTriangle className="h-5 w-5" aria-hidden="true" />
                </div>
              )}
              <div className="space-y-1">
                <Dialog.Title className="text-sm font-semibold text-foreground">
                  {title}
                </Dialog.Title>
                <Dialog.Description className="text-xs text-muted-foreground leading-relaxed">
                  {description}
                </Dialog.Description>
              </div>
            </div>

            <Dialog.Close asChild>
              <Button
                variant="secondary"
                className="h-7 w-7 p-0 shrink-0"
                aria-label={tCommon("close")}
                disabled={pending}
              >
                <X className="h-4 w-4" />
              </Button>
            </Dialog.Close>
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
            <Dialog.Close asChild>
              <Button variant="secondary" disabled={pending}>
                {cancelLabel ?? t("cancel")}
              </Button>
            </Dialog.Close>
            <Button
              variant={destructive ? "danger" : "primary"}
              onClick={handleConfirm}
              disabled={pending}
              className="gap-1.5"
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
