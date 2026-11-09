import type { ReactNode } from "react";

import { SettingsNav } from "@/components/layout/settings-nav";

export const dynamic = "force-dynamic";

export default function SettingsLayout({ children }: { children: ReactNode }) {
  return (
    <div className="mx-auto grid w-full max-w-5xl gap-6 md:grid-cols-[12rem_1fr]">
      <SettingsNav />
      <div className="min-w-0">{children}</div>
    </div>
  );
}
