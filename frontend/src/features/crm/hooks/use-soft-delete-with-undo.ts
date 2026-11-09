"use client";

import { useUndoToast } from "@/lib/toasts";

interface Options {
  deleteEntity: () => Promise<void>;
  restoreEntity: () => Promise<unknown>;
  deletedMessage: string;
  onDeleted?: () => void;
  onRestored?: () => void;
}

export function useSoftDeleteWithUndo({
  deleteEntity,
  restoreEntity,
  deletedMessage,
  onDeleted,
  onRestored,
}: Options) {
  const undoToast = useUndoToast();

  async function handleDelete() {
    await deleteEntity();
    onDeleted?.();
    undoToast({
      message: deletedMessage,
      onUndo: async () => {
        await restoreEntity();
        onRestored?.();
      },
    });
  }

  return { handleDelete };
}
