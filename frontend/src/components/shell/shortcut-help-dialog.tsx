"use client";

import { Keyboard } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect } from "react";

import { Modal } from "@/components/ui/modal";
import { SHELL_SHORTCUTS } from "@/constants/shortcuts";
import { formatKeyCombo, shortcutRegistry } from "@/lib/shortcuts/registry";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ShortcutHelpDialog({ open, onOpenChange }: Props) {
  const t = useTranslations("shell");

  useEffect(() => {
    const unregister = shortcutRegistry.register({
      ...SHELL_SHORTCUTS.help,
      action: () => onOpenChange(true),
    });

    return unregister;
  }, [onOpenChange]);

  const allShortcuts = shortcutRegistry.getAll();
  const generalShortcuts = allShortcuts.filter((s) => s.category === "general");
  const actionShortcuts = allShortcuts.filter((s) => s.category === "actions");
  const navShortcuts = allShortcuts.filter((s) => s.category === "navigation");

  return (
    <Modal
      open={open}
      onOpenChange={onOpenChange}
      size="lg"
      title={t("helpDialog.title")}
      description={t("helpDialog.description")}
      icon={<Keyboard className="h-5 w-5 text-muted-foreground" />}
    >
      <div className="space-y-4">
        {generalShortcuts.length > 0 && (
          <div>
            <h3 className="text-xs font-medium text-muted-foreground mb-2">
              {t("helpDialog.general")}
            </h3>
            <div className="space-y-1.5">
              {generalShortcuts.map((s) => (
                <div
                  key={s.id}
                  className="flex items-center justify-between py-1 text-sm border-b border-border/40"
                >
                  <span>{t(s.titleKey)}</span>
                  <kbd className="rounded-[var(--radius-control)] border border-border bg-background px-2 py-0.5 text-xs font-mono text-muted-foreground">
                    {formatKeyCombo(s.key, {
                      ctrlOrMeta: s.ctrlOrMeta,
                      alt: s.alt,
                      shift: s.shift,
                    })}
                  </kbd>
                </div>
              ))}
            </div>
          </div>
        )}

        {actionShortcuts.length > 0 && (
          <div>
            <h3 className="text-xs font-medium text-muted-foreground mb-2">
              {t("helpDialog.actions")}
            </h3>
            <div className="space-y-1.5">
              {actionShortcuts.map((s) => (
                <div
                  key={s.id}
                  className="flex items-center justify-between py-1 text-sm border-b border-border/40"
                >
                  <span>{t(s.titleKey)}</span>
                  <kbd className="rounded-[var(--radius-control)] border border-border bg-background px-2 py-0.5 text-xs font-mono text-muted-foreground">
                    {formatKeyCombo(s.key, {
                      ctrlOrMeta: s.ctrlOrMeta,
                      alt: s.alt,
                      shift: s.shift,
                    })}
                  </kbd>
                </div>
              ))}
            </div>
          </div>
        )}

        {navShortcuts.length > 0 && (
          <div>
            <h3 className="text-xs font-medium text-muted-foreground mb-2">
              {t("helpDialog.navigation")}
            </h3>
            <div className="space-y-1.5">
              {navShortcuts.map((s) => (
                <div
                  key={s.id}
                  className="flex items-center justify-between py-1 text-sm border-b border-border/40"
                >
                  <span>{t(s.titleKey)}</span>
                  <kbd className="rounded-[var(--radius-control)] border border-border bg-background px-2 py-0.5 text-xs font-mono text-muted-foreground">
                    {formatKeyCombo(s.key, {
                      ctrlOrMeta: s.ctrlOrMeta,
                      alt: s.alt,
                      shift: s.shift,
                    })}
                  </kbd>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
}
