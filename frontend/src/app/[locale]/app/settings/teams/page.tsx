import { getTranslations } from "next-intl/server";

import { TeamsPanel } from "@/features/teams/components/teams-panel";

export default async function SettingsTeamsPanelPage() {
  const t = await getTranslations("settings.teams");

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">{t("title")}</h1>
      <TeamsPanel />
    </section>
  );
}
