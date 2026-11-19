"use client";

import {
  useEffect,
  useState,
  useSyncExternalStore,
  type ReactNode,
} from "react";

import { SIDEBAR_MODES, SIDEBAR_RAIL_QUERY } from "@/constants/shell";
import { writeSidebarPreference } from "@/lib/shell/sidebar-preference";
import { shortcutRegistry } from "@/lib/shortcuts/registry";
import { useRegisteredCreateTargets } from "@/lib/shortcuts/create-target";
import { RouteTransition } from "./route-transition";
import { CommandPalette } from "./command-palette";
import { QuickCreateDialog } from "./quick-create-dialog";
import { ShortcutHelpDialog } from "./shortcut-help-dialog";
import { Sidebar } from "./sidebar";
import { TopBar } from "./top-bar";

interface Props {
  children: ReactNode;
  /** The saved choice, read from the cookie on the server; `null` if none. */
  initialSidebarCollapsed: boolean | null;
}

function useIsTablet(): boolean {
  return useSyncExternalStore(
    (callback) => {
      const mql = window.matchMedia(SIDEBAR_RAIL_QUERY);
      mql.addEventListener("change", callback);
      return () => mql.removeEventListener("change", callback);
    },
    () => window.matchMedia(SIDEBAR_RAIL_QUERY).matches,
    () => false,
  );
}

export function AppShell({ children, initialSidebarCollapsed }: Props) {
  const isTablet = useIsTablet();
  const [userCollapsed, setUserCollapsed] = useState<boolean | null>(
    initialSidebarCollapsed,
  );
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [helpOpen, setHelpOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const targets = useRegisteredCreateTargets();

  // The wording of the toggle follows this; the width itself is CSS (`mode`), so
  // the server's first paint is already right and nothing moves after hydration.
  const isCollapsed = userCollapsed ?? isTablet;
  const sidebarMode =
    userCollapsed === null
      ? SIDEBAR_MODES.auto
      : userCollapsed
        ? SIDEBAR_MODES.collapsed
        : SIDEBAR_MODES.expanded;

  // Global keydown listener
  useEffect(() => {
    const handleKeyDown = (event: KeyboardEvent) => {
      shortcutRegistry.handleKeyDown(event);
    };

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, []);

  return (
    <div className="flex min-h-screen bg-background text-foreground antialiased">
      <Sidebar
        mode={sidebarMode}
        collapsed={isCollapsed}
        onCollapsedChange={(val) => {
          setUserCollapsed(val);
          writeSidebarPreference(val);
        }}
        mobileOpen={mobileOpen}
        onMobileOpenChange={setMobileOpen}
      />

      <div className="flex flex-1 flex-col min-w-0">
        <TopBar
          onOpenSearch={() => setSearchOpen(true)}
          onOpenCreate={
            targets.length > 0 ? () => setCreateOpen(true) : undefined
          }
          onToggleSidebar={() => setMobileOpen(!mobileOpen)}
        />

        <main className="flex-1 p-4 sm:p-6 lg:p-8 max-w-7xl w-full mx-auto">
          <RouteTransition depth={1}>{children}</RouteTransition>
        </main>
      </div>

      <CommandPalette
        open={searchOpen}
        onOpenChange={setSearchOpen}
        onOpenHelp={() => setHelpOpen(true)}
        onOpenCreate={
          targets.length > 0 ? () => setCreateOpen(true) : undefined
        }
      />

      <ShortcutHelpDialog open={helpOpen} onOpenChange={setHelpOpen} />
      <QuickCreateDialog open={createOpen} onOpenChange={setCreateOpen} />
    </div>
  );
}
