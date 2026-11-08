import { getTranslations } from "next-intl/server";

import { MembersPanel } from "@/features/members/components/members-panel";

export default async function SettingsMembersPanelPage() {
  const t = await getTranslations("settings.members");

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">{t("title")}</h1>
      <MembersPanel />
    </section>
  );
}
