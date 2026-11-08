"use client";

import * as Dialog from "@radix-ui/react-dialog";
import { Keyboard, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect } from "react";

import { Button } from "@/components/ui/button";
import { SHELL_SHORTCUTS } from "@/constants/shortcuts";
import { formatKeyCombo, shortcutRegistry } from "@/lib/shortcuts/registry";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function ShortcutHelpDialog({ open, onOpenChange }: Props) {
  const t = useTranslations("shell");
  const tCommon = useTranslations("common");

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
    <Dialog.Root open={open} onOpenChange={onOpenChange}>
      <Dialog.Portal>
        <Dialog.Overlay className="fixed inset-0 z-50 bg-foreground/30 backdrop-blur-sm" />
        <Dialog.Content className="fixed left-1/2 top-1/2 z-50 w-full max-w-lg -translate-x-1/2 -translate-y-1/2 rounded-[var(--radius-card)] border border-border bg-card p-6 shadow-floating space-y-6">
          <div className="flex items-start justify-between">
            <div className="flex items-center gap-2">
              <Keyboard className="h-5 w-5 text-muted-foreground" />
              <div>
                <Dialog.Title className="text-base font-semibold text-foreground">
                  {t("helpDialog.title")}
                </Dialog.Title>
                <Dialog.Description className="text-xs text-muted-foreground">
                  {t("helpDialog.description")}
                </Dialog.Description>
              </div>
            </div>
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

          <div className="space-y-4 max-h-96 overflow-y-auto pr-1">
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
        </Dialog.Content>
      </Dialog.Portal>
    </Dialog.Root>
  );
}
