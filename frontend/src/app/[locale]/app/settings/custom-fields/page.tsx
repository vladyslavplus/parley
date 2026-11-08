import { getTranslations } from "next-intl/server";

import { CustomFieldsSettingsPanel } from "@/features/crm/components/custom-fields-settings-panel";

export default async function SettingsCustomFieldsPage() {
  const t = await getTranslations("settings.customFields");

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">{t("title")}</h1>
      <CustomFieldsSettingsPanel />
    </section>
  );
}
