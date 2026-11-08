"use client";

import { Command } from "cmdk";
import { History, LayoutDashboard, Moon, Search, Sun } from "lucide-react";
import { useTranslations } from "next-intl";
import { useTheme } from "next-themes";
import { useEffect } from "react";

import { SHELL_SHORTCUTS } from "@/constants/shortcuts";
import { Can } from "@/features/session/components/can";
import { useRouter } from "@/i18n/navigation";
import { useRegisteredCommands } from "@/lib/shortcuts/command-source";
import { shortcutRegistry } from "@/lib/shortcuts/registry";
import {
  DEV_NAV_ITEMS,
  SETTINGS_NAV_ITEMS,
  type NavItemConfig,
} from "./nav-items";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onOpenHelp?: () => void;
  onOpenCreate?: () => void;
}

export function CommandPalette({
  open,
  onOpenChange,
  onOpenHelp,
  onOpenCreate,
}: Props) {
  const t = useTranslations("shell");
  const router = useRouter();
  const { setTheme } = useTheme();

  useEffect(() => {
    const unregisterCtrlK = shortcutRegistry.register({
      ...SHELL_SHORTCUTS.palette,
      action: () => onOpenChange(!open),
    });

    const unregisterSlash = shortcutRegistry.register({
      ...SHELL_SHORTCUTS.search,
      action: () => onOpenChange(true),
    });

    return () => {
      unregisterCtrlK();
      unregisterSlash();
    };
  }, [open, onOpenChange]);

  const extraCommands = useRegisteredCommands();

  function runAndClose(action: () => void) {
    onOpenChange(false);
    action();
  }

  function renderNavItem(item: NavItemConfig) {
    const Icon = item.icon;
    const content = (
      <Command.Item
        key={item.id}
        value={t(item.titleKey as "sidebar.items.settings")}
        onSelect={() => runAndClose(() => router.push(item.href))}
        className="flex cursor-pointer items-center gap-2.5 rounded-[var(--radius-control)] px-3 py-2 text-sm text-foreground hover:bg-muted aria-selected:bg-muted transition-colors"
      >
        <Icon className="h-4 w-4 text-muted-foreground" />
        <span className="flex-1">
          {t(item.titleKey as "sidebar.items.settings")}
        </span>
        <span className="text-xs font-mono text-muted-foreground">
          {item.href}
        </span>
      </Command.Item>
    );

    if (item.permission) {
      return (
        <Can key={item.id} permission={item.permission}>
          {content}
        </Can>
      );
    }
    return content;
  }

  return (
    <Command.Dialog
      open={open}
      onOpenChange={onOpenChange}
      label={t("search.placeholder")}
      className="fixed inset-0 z-50 flex items-start justify-center pt-20 sm:pt-28 bg-foreground/30 backdrop-blur-sm p-4"
    >
      <div className="w-full max-w-xl rounded-[var(--radius-card)] border border-border bg-card shadow-floating overflow-hidden">
        <div className="flex items-center border-b border-border px-3 py-2">
          <Search className="mr-2 h-4 w-4 shrink-0 text-muted-foreground" />
          <Command.Input
            autoFocus
            placeholder={t("search.placeholder")}
            className="flex h-10 w-full rounded-[var(--radius-control)] bg-transparent py-2 text-sm outline-none placeholder:text-muted-foreground disabled:cursor-not-allowed disabled:opacity-50"
          />
        </div>

        <Command.List className="max-h-80 overflow-y-auto p-2 space-y-2">
          <Command.Empty className="py-6 text-center text-xs text-muted-foreground">
            {t("commandPalette.noResults")}
          </Command.Empty>

          {/* Quick Actions */}
          <Command.Group
            heading={t("commandPalette.actions")}
            className="px-2 py-1.5 text-xs font-medium text-muted-foreground"
          >
            {onOpenCreate && (
              <Command.Item
                value={t("quickCreate.title")}
                onSelect={() => runAndClose(onOpenCreate)}
                className="flex cursor-pointer items-center gap-2.5 rounded-[var(--radius-control)] px-3 py-2 text-sm text-foreground hover:bg-muted aria-selected:bg-muted transition-colors"
              >
                <LayoutDashboard className="h-4 w-4 text-muted-foreground" />
                <span className="flex-1">{t("quickCreate.title")}</span>
                <kbd className="rounded-[var(--radius-control)] border border-border bg-background px-1.5 py-0.5 text-[10px] font-mono text-muted-foreground">
                  C
                </kbd>
              </Command.Item>
            )}

            {onOpenHelp && (
              <Command.Item
                value={t("shortcuts.help")}
                onSelect={() => runAndClose(onOpenHelp)}
                className="flex cursor-pointer items-center gap-2.5 rounded-[var(--radius-control)] px-3 py-2 text-sm text-foreground hover:bg-muted aria-selected:bg-muted transition-colors"
              >
                <History className="h-4 w-4 text-muted-foreground" />
                <span className="flex-1">{t("shortcuts.help")}</span>
                <kbd className="rounded-[var(--radius-control)] border border-border bg-background px-1.5 py-0.5 text-[10px] font-mono text-muted-foreground">
                  ?
                </kbd>
              </Command.Item>
            )}

            <Command.Item
              value={t("commandPalette.switchToLightTheme")}
              onSelect={() => runAndClose(() => setTheme("light"))}
              className="flex cursor-pointer items-center gap-2.5 rounded-[var(--radius-control)] px-3 py-2 text-sm text-foreground hover:bg-muted aria-selected:bg-muted transition-colors"
            >
              <Sun className="h-4 w-4 text-muted-foreground" />
              <span className="flex-1">{t("commandPalette.lightTheme")}</span>
            </Command.Item>

            <Command.Item
              value={t("commandPalette.switchToDarkTheme")}
              onSelect={() => runAndClose(() => setTheme("dark"))}
              className="flex cursor-pointer items-center gap-2.5 rounded-[var(--radius-control)] px-3 py-2 text-sm text-foreground hover:bg-muted aria-selected:bg-muted transition-colors"
            >
              <Moon className="h-4 w-4 text-muted-foreground" />
              <span className="flex-1">{t("commandPalette.darkTheme")}</span>
            </Command.Item>
          </Command.Group>

          {/* Pluggable external commands */}
          {extraCommands.length > 0 && (
            <Command.Group
              heading={t("commandPalette.additionalCommands")}
              className="px-2 py-1.5 text-xs font-medium text-muted-foreground"
            >
              {extraCommands.map((cmd) => (
                <Command.Item
                  key={cmd.id}
                  value={cmd.title}
                  onSelect={() => runAndClose(cmd.onSelect)}
                  className="flex cursor-pointer items-center gap-2.5 rounded-[var(--radius-control)] px-3 py-2 text-sm text-foreground hover:bg-muted aria-selected:bg-muted transition-colors"
                >
                  <span className="flex-1">{cmd.title}</span>
                  {cmd.shortcut && (
                    <kbd className="rounded-[var(--radius-control)] border border-border bg-background px-1.5 py-0.5 text-[10px] font-mono text-muted-foreground">
                      {cmd.shortcut}
                    </kbd>
                  )}
                </Command.Item>
              ))}
            </Command.Group>
          )}

          {/* Settings Navigation */}
          <Command.Group
            heading={t("commandPalette.settings")}
            className="px-2 py-1.5 text-xs font-medium text-muted-foreground"
          >
            {SETTINGS_NAV_ITEMS.map(renderNavItem)}
          </Command.Group>

          {/* Dev Tools (in dev only) */}
          {process.env.NODE_ENV !== "production" && (
            <Command.Group
              heading={t("commandPalette.development")}
              className="px-2 py-1.5 text-xs font-medium text-muted-foreground"
            >
              {DEV_NAV_ITEMS.map(renderNavItem)}
            </Command.Group>
          )}
        </Command.List>
      </div>
    </Command.Dialog>
  );
}
