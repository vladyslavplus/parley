"use client";

import * as Dialog from "@radix-ui/react-dialog";
import { useQueryClient } from "@tanstack/react-query";
import { ChevronLeft, ChevronRight } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useEffect } from "react";

import { LogoMark } from "@/components/brand/logo-mark";
import { Button } from "@/components/ui/button";
import { PERMISSIONS } from "@/constants/permissions";
import { ROUTES } from "@/constants/routes";
import { LIMITS } from "@/constants/limits";
import { SHELL_SHORTCUTS } from "@/constants/shortcuts";
import { companyListQueryOptions } from "@/features/crm/hooks/use-companies";
import { contactListQueryOptions } from "@/features/crm/hooks/use-contacts";
import { useOverdueTasksCount } from "@/features/tasks/hooks/use-tasks";
import {
  isOverdueCountCapped,
  overdueBadgeText,
} from "@/features/tasks/lib/overdue-badge";
import { Can, useCan } from "@/features/session/components/can";
import { Link, usePathname } from "@/i18n/navigation";
import { shortcutRegistry } from "@/lib/shortcuts/registry";
import { cn } from "@/lib/utils";
import {
  DEV_NAV_ITEMS,
  MAIN_NAV_ITEMS,
  SETTINGS_NAV_ITEMS,
  type NavItemConfig,
} from "./nav-items";

const LOGO_SIZE = 28;

interface Props {
  collapsed: boolean;
  onCollapsedChange: (collapsed: boolean) => void;
  mobileOpen: boolean;
  onMobileOpenChange: (open: boolean) => void;
}

export function Sidebar({
  collapsed,
  onCollapsedChange,
  mobileOpen,
  onMobileOpenChange,
}: Props) {
  const tCommon = useTranslations("common");
  const tShell = useTranslations("shell");
  const pathname = usePathname();
  const locale = useLocale();
  const queryClient = useQueryClient();
  const canReadTasks = useCan(PERMISSIONS.tasksRead);
  const { data: overdueCount = 0 } = useOverdueTasksCount({
    enabled: canReadTasks,
  });
  const hasOverdue = overdueCount > 0;
  const overdueText = isOverdueCountCapped(overdueCount)
    ? tShell("sidebar.overdueTasksMany", { count: LIMITS.overdueBadgeCap })
    : tShell("sidebar.overdueTasks", { count: overdueCount });

  useEffect(() => {
    const unregister = shortcutRegistry.register({
      ...SHELL_SHORTCUTS.sidebarToggle,
      action: () => onCollapsedChange(!collapsed),
    });

    return unregister;
  }, [collapsed, onCollapsedChange]);

  function handlePrefetch(item: NavItemConfig) {
    if (item.id === "contacts") {
      void queryClient
        .query(contactListQueryOptions({}, locale))
        .catch(() => {});
    } else if (item.id === "companies") {
      void queryClient
        .query(companyListQueryOptions({}, locale))
        .catch(() => {});
    }
  }

  function renderItem(item: NavItemConfig) {
    const Icon = item.icon;
    const active =
      pathname === item.href ||
      (item.id === "settings" && pathname.startsWith("/app/settings"));

    const label = tShell(item.titleKey as Parameters<typeof tShell>[0]);
    const overdue = item.id === "tasks" && hasOverdue;
    // The hover text names the number; collapsed, it also carries the label.
    const linkTitle = overdue
      ? collapsed
        ? `${label}: ${overdueText}`
        : overdueText
      : collapsed
        ? label
        : undefined;

    const link = (
      <Link
        key={item.id}
        href={item.href}
        prefetch={true}
        onMouseEnter={() => handlePrefetch(item)}
        onFocus={() => handlePrefetch(item)}
        onClick={() => onMobileOpenChange(false)}
        aria-current={active ? "page" : undefined}
        title={linkTitle}
        className={cn(
          "flex items-center gap-3 rounded-[var(--radius-control)] px-3 py-2 text-sm font-medium transition-colors focus-visible:outline-2 focus-visible:outline-ring overflow-hidden",
          active
            ? "bg-muted text-foreground font-semibold"
            : "text-muted-foreground hover:bg-muted hover:text-foreground",
        )}
      >
        <span className="relative flex shrink-0">
          <Icon className="h-4 w-4" />
          {overdue && collapsed ? (
            <span
              className="absolute -right-1 -top-1 h-2 w-2 rounded-full bg-destructive ring-2 ring-card"
              aria-hidden="true"
            />
          ) : null}
        </span>
        <span
          className={cn(
            "truncate whitespace-nowrap transition-opacity duration-150 flex-1",
            collapsed ? "opacity-0 pointer-events-none md:w-0" : "opacity-100",
          )}
        >
          {label}
        </span>
        {overdue ? (
          <>
            {collapsed ? null : (
              <span
                aria-hidden="true"
                className="ml-auto flex h-4 min-w-4 shrink-0 items-center justify-center rounded-full bg-destructive/10 px-1 text-[10px] font-semibold leading-none tabular-nums text-destructive"
              >
                {overdueBadgeText(overdueCount)}
              </span>
            )}
            <span className="sr-only">{overdueText}</span>
          </>
        ) : null}
      </Link>
    );

    if (item.permission) {
      return (
        <Can key={item.id} permission={item.permission}>
          {link}
        </Can>
      );
    }
    return link;
  }

  const sidebarContent = (
    <div className="flex h-full w-56 flex-col justify-between p-3 select-none shrink-0">
      <div className="space-y-6">
        {/* Brand mark pointing to /app */}
        <Link
          href={ROUTES.app}
          className="flex items-center gap-2 px-2 py-1.5 rounded-[var(--radius-control)] hover:opacity-80 transition-opacity"
        >
          <LogoMark size={LOGO_SIZE} />
          <span
            className={cn(
              "font-display font-semibold text-base tracking-[-0.01em] text-foreground whitespace-nowrap transition-opacity duration-150",
              collapsed
                ? "opacity-0 pointer-events-none md:w-0"
                : "opacity-100",
            )}
          >
            {tCommon("appName")}
          </span>
        </Link>

        {/* Main Section */}
        <nav aria-label={tShell("sidebar.navigation")} className="space-y-1">
          {MAIN_NAV_ITEMS.map(renderItem)}
        </nav>

        {/* Settings Section */}
        <nav aria-label={tShell("sidebar.settings")} className="space-y-1">
          {SETTINGS_NAV_ITEMS.map(renderItem)}
        </nav>

        {/* Development Section (dev only) */}
        {process.env.NODE_ENV !== "production" && (
          <div className="space-y-1">
            <h3
              className={cn(
                "px-3 text-xs font-medium text-muted-foreground whitespace-nowrap transition-opacity duration-150",
                collapsed
                  ? "opacity-0 pointer-events-none md:w-0"
                  : "opacity-100",
              )}
            >
              {tShell("sidebar.development")}
            </h3>
            <nav
              aria-label={tShell("sidebar.development")}
              className="space-y-1"
            >
              {DEV_NAV_ITEMS.map(renderItem)}
            </nav>
          </div>
        )}
      </div>

      {/* Collapse Toggle Footer (desktop only) */}
      <div className="hidden border-t border-border pt-2 md:block">
        <Button
          variant="secondary"
          onClick={() => onCollapsedChange(!collapsed)}
          className={cn(
            "h-8 text-xs text-muted-foreground hover:text-foreground overflow-hidden",
            collapsed
              ? "w-10 p-0 justify-center"
              : "w-full justify-between px-3",
          )}
          aria-label={
            collapsed ? tShell("sidebar.expand") : tShell("sidebar.collapse")
          }
        >
          <span
            className={cn(
              "whitespace-nowrap transition-opacity duration-150",
              collapsed ? "opacity-0 pointer-events-none w-0" : "opacity-100",
            )}
          >
            {tShell("sidebar.collapse")}
          </span>
          {collapsed ? (
            <ChevronRight className="h-4 w-4 shrink-0" />
          ) : (
            <ChevronLeft className="h-4 w-4 shrink-0" />
          )}
        </Button>
      </div>
    </div>
  );

  return (
    <>
      {/* Desktop & Tablet Sidebar */}
      <aside
        className={cn(
          "hidden md:flex flex-col sticky top-0 h-dvh border-r border-border bg-card overflow-hidden transition-[width] duration-200 motion-reduce:transition-none shrink-0",
          collapsed ? "w-16" : "w-56",
        )}
      >
        {sidebarContent}
      </aside>

      {/* Mobile Drawer Backdrop + Sheet (Radix Dialog) */}
      <Dialog.Root open={mobileOpen} onOpenChange={onMobileOpenChange}>
        <Dialog.Portal>
          <Dialog.Overlay className="fixed inset-0 z-50 flex md:hidden bg-foreground/40 backdrop-blur-sm" />
          <Dialog.Content className="fixed inset-y-0 left-0 z-50 w-64 max-w-[80vw] bg-card border-r border-border shadow-floating p-2 focus:outline-none">
            <Dialog.Title className="sr-only">
              {tCommon("appName")}
            </Dialog.Title>
            {sidebarContent}
          </Dialog.Content>
        </Dialog.Portal>
      </Dialog.Root>
    </>
  );
}
