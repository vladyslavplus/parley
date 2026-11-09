import type { ReactNode } from "react";

import { AppShell } from "@/components/shell/app-shell";
import { CrmShellRegistration } from "@/features/crm/components/crm-shell-registration";
import { DealShellRegistration } from "@/features/deals/components/deal-shell-registration";
import { TaskShellRegistration } from "@/features/tasks/components/task-shell-registration";

export default function AppLayout({ children }: { children: ReactNode }) {
  return (
    <AppShell>
      <CrmShellRegistration />
      <DealShellRegistration />
      <TaskShellRegistration />
      {children}
    </AppShell>
  );
}
