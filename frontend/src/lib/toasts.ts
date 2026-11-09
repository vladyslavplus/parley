import { useTranslations } from "next-intl";
import { toast, type ExternalToast } from "sonner";

import { LIMITS } from "@/constants/limits";

export interface UndoToastOptions {
  message: string;
  description?: string;
  undoLabel: string;
  failedMessage: string;
  duration?: number;
  onUndo: () => void | Promise<void>;
}

export function showSuccessToast(
  message: string,
  options?: ExternalToast,
): string | number {
  return toast.success(message, options);
}

export function showErrorToast(
  message: string,
  options?: ExternalToast,
): string | number {
  return toast.error(message, {
    duration: LIMITS.toastErrorDurationMs,
    ...options,
  });
}

export function showInfoToast(
  message: string,
  options?: ExternalToast,
): string | number {
  return toast.info(message, options);
}

export function showWarningToast(
  message: string,
  options?: ExternalToast,
): string | number {
  return toast.warning(message, options);
}

export function dismissToast(id?: string | number): string | number {
  return toast.dismiss(id);
}

export function showUndoToast({
  message,
  description,
  undoLabel,
  failedMessage,
  duration = LIMITS.toastUndoDurationMs,
  onUndo,
}: UndoToastOptions): string | number {
  return toast(message, {
    description,
    duration,
    action: {
      label: undoLabel,
      onClick: async () => {
        try {
          await onUndo();
        } catch {
          showErrorToast(failedMessage);
        }
      },
    },
  });
}

export function useUndoToast() {
  const t = useTranslations("common");

  return (
    options: Omit<UndoToastOptions, "undoLabel" | "failedMessage"> & {
      undoLabel?: string;
      failedMessage?: string;
    },
  ) => {
    return showUndoToast({
      undoLabel: options.undoLabel ?? t("undo"),
      failedMessage: options.failedMessage ?? t("undoFailed"),
      ...options,
    });
  };
}

export type { ExternalToast };
