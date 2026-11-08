import { useTranslations } from "next-intl";
import { toast } from "sonner";

import { LIMITS } from "@/constants/limits";

export interface UndoToastOptions {
  message: string;
  description?: string;
  undoLabel: string;
  failedMessage: string;
  duration?: number;
  onUndo: () => void | Promise<void>;
}

export function showUndoToast({
  message,
  description,
  undoLabel,
  failedMessage,
  duration = LIMITS.toastDurationMs,
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
          toast.error(failedMessage);
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

export { toast };
