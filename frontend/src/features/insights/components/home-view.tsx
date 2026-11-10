"use client";

import { useTranslations } from "next-intl";

import type { HomeSections } from "@/features/insights/lib/home-sections";
import { DashboardWidgets } from "@/features/insights/components/dashboard-widgets";
import { PriorityPanel } from "@/features/insights/components/priority-panel";

// The home screen. The sections were decided on the server from the user's
// permissions; each one renders only when the user may read its data.
export function HomeView({ sections }: { sections: HomeSections }) {
  const t = useTranslations("insights.home");
  return (
    <div className="space-y-8">
      <div>
        <h1 className="text-xl font-bold tracking-tight text-foreground">
          {t("title")}
        </h1>
        <p className="mt-0.5 text-xs text-muted-foreground">{t("subtitle")}</p>
      </div>
      {sections.priority ? <PriorityPanel /> : null}
      {sections.dashboard ? <DashboardWidgets /> : null}
    </div>
  );
}
