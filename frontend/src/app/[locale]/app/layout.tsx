import type { ReactNode } from "react";

import { AppShell } from "@/components/shell/app-shell";
import { CrmShellRegistration } from "@/features/crm/components/crm-shell-registration";

export default function AppLayout({ children }: { children: ReactNode }) {
  return (
    <AppShell>
      <CrmShellRegistration />
      {children}
    </AppShell>
  );
}
