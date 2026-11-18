"use client";

import { Plus } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect } from "react";

import { Modal } from "@/components/ui/modal";
import { SHELL_SHORTCUTS } from "@/constants/shortcuts";
import { useRegisteredCreateTargets } from "@/lib/shortcuts/create-target";
import { shortcutRegistry } from "@/lib/shortcuts/registry";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function QuickCreateDialog({ open, onOpenChange }: Props) {
  const t = useTranslations("shell.quickCreate");
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
    <Modal open={open} onOpenChange={onOpenChange} title={t("title")} size="sm">
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
    </Modal>
  );
}
