"use client";

import {
  useEffect,
  useState,
  useSyncExternalStore,
  type ReactNode,
} from "react";

import { shortcutRegistry } from "@/lib/shortcuts/registry";
import { useRegisteredCreateTargets } from "@/lib/shortcuts/create-target";
import { CommandPalette } from "./command-palette";
import { QuickCreateDialog } from "./quick-create-dialog";
import { ShortcutHelpDialog } from "./shortcut-help-dialog";
import { Sidebar } from "./sidebar";
import { TopBar } from "./top-bar";

interface Props {
  children: ReactNode;
}

function useIsTablet(): boolean {
  return useSyncExternalStore(
    (callback) => {
      const mql = window.matchMedia("(max-width: 1024px)");
      mql.addEventListener("change", callback);
      return () => mql.removeEventListener("change", callback);
    },
    () => window.matchMedia("(max-width: 1024px)").matches,
    () => false,
  );
}

export function AppShell({ children }: Props) {
  const isTablet = useIsTablet();
  const [userCollapsed, setUserCollapsed] = useState<boolean | null>(null);
  const [mobileOpen, setMobileOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [helpOpen, setHelpOpen] = useState(false);
  const [createOpen, setCreateOpen] = useState(false);
  const targets = useRegisteredCreateTargets();

  const isCollapsed = userCollapsed ?? isTablet;

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
        collapsed={isCollapsed}
        onCollapsedChange={(val) => setUserCollapsed(val)}
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
          {children}
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
