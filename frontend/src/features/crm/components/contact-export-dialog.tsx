"use client";

import {
  AlertTriangle,
  CheckCircle2,
  Download,
  FileSpreadsheet,
  Loader2,
  RefreshCw,
  X,
} from "lucide-react";
import { useTranslations } from "next-intl";
import * as Dialog from "@radix-ui/react-dialog";

import { Button } from "@/components/ui/button";
import { useContactExport } from "@/features/crm/hooks/use-contact-export";

export interface ContactExportDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  filters: {
    q?: string;
    tagId?: string;
    ownerUserId?: string;
  };
}

export function ContactExportDialog({
  open,
  onOpenChange,
  filters,
}: ContactExportDialogProps) {
  const t = useTranslations("crm.export");
  const tCommon = useTranslations("common");
  const { state, startExport, reset } = useContactExport();

  const handleOpenChange = (nextOpen: boolean) => {
    if (!nextOpen) {
      reset();
    }
    onOpenChange(nextOpen);
  };

  const isPending =
    state.status === "starting" ||
    state.status === "queued" ||
    state.status === "running";

  return (
    <Dialog.Root open={open} onOpenChange={handleOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/30 backdrop-blur-sm" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-full max-w-md -translate-x-1/2 -translate-y-1/2 rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-floating space-y-4">
          <div className="flex items-start justify-between gap-3">
            <div className="flex items-center gap-2">
              <div className="flex h-9 w-9 items-center justify-center rounded-[var(--radius-control)] bg-primary/10 text-primary">
                <FileSpreadsheet className="h-5 w-5" />
              </div>
              <div>
                <Dialog.Title className="text-base font-semibold text-foreground">
                  {t("dialogTitle")}
                </Dialog.Title>
                <Dialog.Description className="text-xs text-muted-foreground">
                  {t("dialogDescription")}
                </Dialog.Description>
              </div>
            </div>
            <Dialog.Close asChild>
              <Button
                variant="secondary"
                className="h-7 w-7 p-0"
                aria-label={tCommon("cancel")}
              >
                <X className="h-4 w-4" />
              </Button>
            </Dialog.Close>
          </div>

          {/* Screen reader live announcement */}
          <div className="sr-only" aria-live="polite" role="status">
            {state.status === "starting" && t("status.starting")}
            {state.status === "queued" && t("status.queued")}
            {state.status === "running" && t("status.running")}
            {state.status === "completed" &&
              t("status.completed", { count: state.rowCount ?? 0 })}
            {state.status === "failed" &&
              (state.errorMessage ?? t("status.failed"))}
            {state.status === "expired" && t("status.expired")}
          </div>

          {/* Body content based on state */}
          <div className="py-2">
            {state.status === "idle" && (
              <div className="space-y-3">
                <p className="text-xs text-muted-foreground leading-relaxed">
                  {t("confirmExplanation")}
                </p>
                <div className="flex items-center justify-end gap-2 pt-2">
                  <Button
                    variant="secondary"
                    className="px-3 text-xs"
                    onClick={() => handleOpenChange(false)}
                  >
                    {tCommon("cancel")}
                  </Button>
                  <Button
                    variant="primary"
                    className="px-3 text-xs"
                    onClick={() => void startExport(filters)}
                  >
                    {t("start")}
                  </Button>
                </div>
              </div>
            )}

            {isPending && (
              <div className="flex flex-col items-center justify-center py-6 text-center space-y-3">
                <Loader2 className="h-8 w-8 animate-spin text-primary" />
                <div className="space-y-1">
                  <p className="text-sm font-medium text-foreground">
                    {state.status === "starting" && t("status.starting")}
                    {state.status === "queued" && t("status.queued")}
                    {state.status === "running" && t("status.running")}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {t("inProgressHelp")}
                  </p>
                </div>
              </div>
            )}

            {state.status === "completed" && (
              <div className="flex flex-col items-center justify-center py-4 text-center space-y-4">
                <div className="flex h-12 w-12 items-center justify-center rounded-full bg-success/10 text-success">
                  <CheckCircle2 className="h-6 w-6" />
                </div>
                <div className="space-y-1">
                  <p className="text-sm font-medium text-foreground">
                    {t("status.completed", { count: state.rowCount ?? 0 })}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {t("readyToDownload")}
                  </p>
                </div>
                <div className="flex items-center gap-2 pt-2">
                  <Button
                    variant="secondary"
                    className="px-3 text-xs"
                    onClick={() => handleOpenChange(false)}
                  >
                    {t("close")}
                  </Button>
                  {state.downloadUrl && (
                    <a
                      href={state.downloadUrl}
                      download
                      className="inline-flex items-center gap-1.5 px-3 rounded-[var(--radius-control)] bg-primary text-primary-foreground text-xs font-medium hover:bg-primary/90 transition-colors"
                    >
                      <Download className="h-3.5 w-3.5" />
                      <span>{t("download")}</span>
                    </a>
                  )}
                </div>
              </div>
            )}

            {(state.status === "failed" || state.status === "expired") && (
              <div className="flex flex-col items-center justify-center py-4 text-center space-y-3">
                <div className="flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10 text-destructive">
                  <AlertTriangle className="h-6 w-6" />
                </div>
                <div className="space-y-1">
                  <p className="text-sm font-medium text-destructive">
                    {state.status === "expired"
                      ? t("status.expired")
                      : (state.errorMessage ?? t("status.failed"))}
                  </p>
                  <p className="text-xs text-muted-foreground">
                    {t("tryAgainHelp")}
                  </p>
                </div>
                <div className="flex items-center gap-2 pt-2">
                  <Button
                    variant="secondary"
                    className="px-3 text-xs"
                    onClick={() => handleOpenChange(false)}
                  >
                    {t("close")}
                  </Button>
                  <Button
                    variant="primary"
                    onClick={() => void startExport(filters)}
                    className="px-3 text-xs gap-1.5"
                  >
                    <RefreshCw className="h-3.5 w-3.5" />
                    <span>{t("tryAgain")}</span>
                  </Button>
                </div>
              </div>
            )}
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
