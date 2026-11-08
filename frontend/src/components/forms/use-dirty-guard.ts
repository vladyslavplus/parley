"use client";

import { useEffect, useState } from "react";

export function useDirtyGuard(isDirty: boolean) {
  const [showPrompt, setShowPrompt] = useState(false);
  const [pendingAction, setPendingAction] = useState<(() => void) | null>(null);

  useEffect(() => {
    if (!isDirty) return;

    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      e.preventDefault();
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [isDirty]);

  function guardedAction(action: () => void) {
    if (isDirty) {
      setPendingAction(() => action);
      setShowPrompt(true);
    } else {
      action();
    }
  }

  function confirmDiscard() {
    setShowPrompt(false);
    if (pendingAction) {
      pendingAction();
      setPendingAction(null);
    }
  }

  function cancelDiscard() {
    setShowPrompt(false);
    setPendingAction(null);
  }

  return {
    showPrompt,
    guardedAction,
    confirmDiscard,
    cancelDiscard,
  };
}
