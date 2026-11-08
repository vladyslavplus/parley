"use client";

import * as Dialog from "@radix-ui/react-dialog";
import { Plus, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect } from "react";

import { Button } from "@/components/ui/button";
import { SHELL_SHORTCUTS } from "@/constants/shortcuts";
import { useRegisteredCreateTargets } from "@/lib/shortcuts/create-target";
import { shortcutRegistry } from "@/lib/shortcuts/registry";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function QuickCreateDialog({ open, onOpenChange }: Props) {
  const t = useTranslations("shell.quickCreate");
  const tCommon = useTranslations("common");
  const targets = useRegisteredCreateTargets();

  useEffect(() => {
    if (targets.length === 0) return;
    const unregister = shortcutRegistry.register({
      ...SHELL_SHORTCUTS.quickCreate,
      action: () => onOpenChange(true),
    });

    return unregister;
  }, [targets.length, onOpenChange]);

  if (targets.length === 0) {
    return null;
  }

  return (
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/30 backdrop-blur-sm" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-full max-w-sm -translate-x-1/2 -translate-y-1/2 rounded-[var(--radius-card)] border border-border bg-card p-5 shadow-floating space-y-4">
          <div className="flex items-center justify-between">
            <Dialog.Title className="text-sm font-semibold text-foreground">
              {t("title")}
            </Dialog.Title>
            <Dialog.Close asChild>
              <Button
                variant="secondary"
                className="h-7 w-7 p-0"
                aria-label={tCommon("close")}
              >
                <X className="h-4 w-4" />
              </Button>
            </Dialog.Close>
          </div>

          <div className="space-y-1">
            {targets.map((tgt) => (
              <button
                key={tgt.id}
                type="button"
                onClick={() => {
                  onOpenChange(false);
                  tgt.action();
                }}
                className="flex w-full items-center gap-2.5 rounded-[var(--radius-control)] px-3 py-2 text-sm text-foreground hover:bg-muted text-left transition-colors cursor-pointer"
              >
                <Plus className="h-4 w-4 text-muted-foreground" />
                <span>{t(tgt.titleKey)}</span>
              </button>
            ))}
          </div>
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
