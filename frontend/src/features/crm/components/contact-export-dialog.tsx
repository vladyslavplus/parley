"use client";

import {
  AlertTriangle,
  CheckCircle2,
  Download,
  FileSpreadsheet,
  Loader2,
  RefreshCw,
} from "lucide-react";
import { useTranslations } from "next-intl";
import type { ReactNode } from "react";

import { Button, buttonVariants } from "@/components/ui/button";
import { Modal } from "@/components/ui/modal";
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

  const closeButton = (
    <Button
      variant="secondary"
      className="px-3 text-xs"
      onClick={() => handleOpenChange(false)}
    >
      {t("close")}
    </Button>
  );

  const retryButton = (
    <Button
      variant="primary"
      onClick={() => void startExport(filters)}
      className="gap-1.5 px-3 text-xs"
    >
      <RefreshCw className="h-3.5 w-3.5" aria-hidden="true" />
      <span>{t("tryAgain")}</span>
    </Button>
  );

  let footer: ReactNode = undefined;
  if (state.status === "idle") {
    footer = (
      <div className="flex justify-end gap-2">
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
    );
  } else if (state.status === "completed") {
    footer = (
      <div className="flex items-center justify-end gap-2">
        {closeButton}
        {state.downloadUrl && (
          <a
            href={state.downloadUrl}
            download
            className={buttonVariants({
              variant: "primary",
              className: "gap-1.5 px-3 text-xs",
            })}
          >
            <Download className="h-3.5 w-3.5" aria-hidden="true" />
            <span>{t("download")}</span>
          </a>
        )}
      </div>
    );
  } else if (state.status === "failed" || state.status === "expired") {
    footer = (
      <div className="flex items-center justify-end gap-2">
        {closeButton}
        {retryButton}
      </div>
    );
  }

  return (
    <Modal
      open={open}
      onOpenChange={handleOpenChange}
      title={t("dialogTitle")}
      description={t("dialogDescription")}
      footer={footer}
      icon={
        <div className="flex h-9 w-9 items-center justify-center rounded-[var(--radius-control)] bg-primary/10 text-primary">
          <FileSpreadsheet className="h-5 w-5" aria-hidden="true" />
        </div>
      }
    >
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

      {state.status === "idle" && (
        <p className="text-xs leading-relaxed text-muted-foreground">
          {t("confirmExplanation")}
        </p>
      )}

      {isPending && (
        <div className="flex flex-col items-center justify-center space-y-3 py-6 text-center">
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
        <div className="flex flex-col items-center justify-center space-y-4 py-4 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-success/10 text-success">
            <CheckCircle2 className="h-6 w-6" aria-hidden="true" />
          </div>
          <div className="space-y-1">
            <p className="text-sm font-medium text-foreground">
              {t("status.completed", { count: state.rowCount ?? 0 })}
            </p>
            <p className="text-xs text-muted-foreground">
              {t("readyToDownload")}
            </p>
          </div>
        </div>
      )}

      {(state.status === "failed" || state.status === "expired") && (
        <div className="flex flex-col items-center justify-center space-y-3 py-4 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10 text-destructive">
            <AlertTriangle className="h-6 w-6" aria-hidden="true" />
          </div>
          <div className="space-y-1">
            <p className="text-sm font-medium text-destructive">
              {state.status === "expired"
                ? t("status.expired")
                : (state.errorMessage ?? t("status.failed"))}
            </p>
            <p className="text-xs text-muted-foreground">{t("tryAgainHelp")}</p>
          </div>
        </div>
      )}
    </Modal>
  );
}
