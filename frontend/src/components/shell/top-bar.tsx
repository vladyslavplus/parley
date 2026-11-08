"use client";

import * as DropdownMenu from "@radix-ui/react-dropdown-menu";
import { useQueryClient } from "@tanstack/react-query";
import {
  Bell,
  LogOut,
  Menu,
  Plus,
  Search,
  User as UserIcon,
} from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "next/navigation";

import { LocaleToggle } from "@/components/layout/locale-toggle";
import { ThemeToggle } from "@/components/layout/theme-toggle";
import { Button } from "@/components/ui/button";
import { API_ROUTES, ROUTES } from "@/constants/routes";
import { OrganizationSwitcher } from "@/features/organization/components/organization-switcher";
import { Link } from "@/i18n/navigation";
import { bffFetch } from "@/lib/api/bff-client";
import { useAction, useMe, useMyOrganizations } from "@/lib/api/hooks/use-api";
import { useRegisteredCreateTargets } from "@/lib/shortcuts/create-target";
import { useIsMac } from "@/lib/shortcuts/registry";

interface Props {
  onOpenSearch: () => void;
  onOpenCreate?: () => void;
  onToggleSidebar: () => void;
}

export function TopBar({ onOpenSearch, onOpenCreate, onToggleSidebar }: Props) {
  const tCommon = useTranslations("common");
  const tShell = useTranslations("shell");
  const locale = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { data: me } = useMe();
  const { data: organizations } = useMyOrganizations();
  const { pending, run } = useAction();
  const isMac = useIsMac();

  const registeredTargets = useRegisteredCreateTargets();

  async function signOut() {
    await run(async () => {
      await bffFetch(API_ROUTES.logout, { method: "POST", locale });
    });
    queryClient.clear();
    router.replace(`/${locale}${ROUTES.login}`);
    router.refresh();
  }

  const defaultInitial = tShell("user.defaultInitial");
  const rawIdentifier =
    me?.user.displayName ?? me?.user.email ?? defaultInitial;
  const userInitials = rawIdentifier.slice(0, 2).toUpperCase();

  return (
    <header className="sticky top-0 z-30 flex h-14 w-full items-center justify-between border-b border-border bg-card/80 px-4 backdrop-blur-md">
      {/* Left: Mobile sidebar toggle + Search trigger */}
      <div className="flex items-center gap-2">
        <Button
          variant="secondary"
          onClick={onToggleSidebar}
          className="h-8 w-8 p-0 md:hidden"
          aria-label={tShell("sidebar.expand")}
        >
          <Menu className="h-4 w-4" />
        </Button>

        <button
          type="button"
          onClick={onOpenSearch}
          className="flex h-9 items-center gap-2 rounded-[var(--radius-control)] border border-input bg-background px-3 text-xs text-muted-foreground hover:border-ring transition-colors sm:w-64"
          aria-label={tShell("search.placeholder")}
        >
          <Search className="h-3.5 w-3.5" />
          <span className="hidden sm:inline">{tShell("search.trigger")}</span>
          <span className="sm:hidden">{tShell("search.trigger")}</span>
          <kbd className="ml-auto hidden rounded-[var(--radius-control)] border border-border bg-card px-1.5 py-0.5 text-[10px] font-mono sm:inline">
            {isMac ? "⌘K" : tShell("search.shortcut")}
          </kbd>
        </button>
      </div>

      {/* Right: Quick Create + Org Switcher + Notifications + User Menu */}
      <div className="flex items-center gap-2 sm:gap-3">
        {onOpenCreate && registeredTargets.length > 0 && (
          <Button
            variant="secondary"
            onClick={onOpenCreate}
            className="h-8 px-2.5 text-xs gap-1 hidden sm:inline-flex"
          >
            <Plus className="h-3.5 w-3.5" />
            <span>{tShell("quickCreate.title")}</span>
          </Button>
        )}

        {me ? (
          <OrganizationSwitcher
            current={me.organization.id}
            options={organizations ?? []}
          />
        ) : null}

        {/* Notifications slot */}
        <Button
          variant="secondary"
          className="h-8 w-8 p-0 text-muted-foreground hover:text-foreground relative"
          aria-label={tShell("notifications.label")}
          title={tShell("notifications.empty")}
        >
          <Bell className="h-4 w-4" />
          <span className="sr-only">{tShell("notifications.label")}</span>
        </Button>

        <LocaleToggle />
        <ThemeToggle />

        {/* User profile & menu using Radix DropdownMenu */}
        {me ? (
          <DropdownMenu.Root>
            <DropdownMenu.Trigger asChild>
              <button
                type="button"
                className="flex h-8 w-8 items-center justify-center rounded-full bg-muted text-foreground text-xs font-semibold hover:opacity-90 transition-opacity focus-visible:outline-2 focus-visible:outline-ring"
                aria-label={me.user.displayName ?? me.user.email}
              >
                {userInitials}
              </button>
            </DropdownMenu.Trigger>
            <DropdownMenu.Portal>
              <DropdownMenu.Content
                align="end"
                sideOffset={8}
                className="z-50 min-w-56 rounded-[var(--radius-card)] border border-border bg-card p-2 shadow-floating text-xs"
              >
                <div className="border-b border-border px-2 py-1.5 mb-1">
                  <div className="font-semibold text-foreground truncate">
                    {me.user.displayName ?? me.user.email}
                  </div>
                  <div className="text-muted-foreground truncate">
                    {me.user.email}
                  </div>
                </div>

                <DropdownMenu.Item asChild>
                  <Link
                    href={ROUTES.settingsProfile}
                    className="flex items-center gap-2 rounded-[var(--radius-control)] px-2 py-1.5 text-foreground hover:bg-muted focus:bg-muted focus:outline-none transition-colors cursor-pointer"
                  >
                    <UserIcon className="h-3.5 w-3.5 text-muted-foreground" />
                    <span>{tShell("userMenu.profile")}</span>
                  </Link>
                </DropdownMenu.Item>

                <DropdownMenu.Item
                  onSelect={signOut}
                  disabled={pending}
                  className="flex w-full items-center gap-2 rounded-[var(--radius-control)] px-2 py-1.5 text-destructive hover:bg-destructive/10 focus:bg-destructive/10 focus:outline-none transition-colors cursor-pointer disabled:opacity-50"
                >
                  <LogOut className="h-3.5 w-3.5" />
                  <span>{tCommon("actions.signOut")}</span>
                </DropdownMenu.Item>
              </DropdownMenu.Content>
            </DropdownMenu.Portal>
          </DropdownMenu.Root>
        ) : null}
      </div>
    </header>
  );
}
