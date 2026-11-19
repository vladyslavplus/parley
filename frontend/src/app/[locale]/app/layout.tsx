import { cookies } from "next/headers";
import type { ReactNode } from "react";

import { AppShell } from "@/components/shell/app-shell";
import { SIDEBAR_PREFERENCE } from "@/constants/shell";
import { CrmShellRegistration } from "@/features/crm/components/crm-shell-registration";
import { DealShellRegistration } from "@/features/deals/components/deal-shell-registration";
import { TaskShellRegistration } from "@/features/tasks/components/task-shell-registration";
import { parseSidebarPreference } from "@/lib/shell/sidebar-preference";

export default async function AppLayout({ children }: { children: ReactNode }) {
  const jar = await cookies();
  const sidebarCollapsed = parseSidebarPreference(
    jar.get(SIDEBAR_PREFERENCE.cookie)?.value,
  );

  return (
    <AppShell initialSidebarCollapsed={sidebarCollapsed}>
      <CrmShellRegistration />
      <DealShellRegistration />
      <TaskShellRegistration />
      {children}
    </AppShell>
  );
}
