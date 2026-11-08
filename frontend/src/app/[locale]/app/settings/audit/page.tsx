import { getTranslations } from "next-intl/server";

import { AuditPanel } from "@/features/audit/components/audit-panel";

export default async function SettingsAuditPanelPage() {
  const t = await getTranslations("settings.audit");

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">{t("title")}</h1>
      <AuditPanel />
    </section>
  );
}
