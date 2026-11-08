import { getTranslations } from "next-intl/server";

import { OrganizationForm } from "@/features/organization/components/organization-form";

export default async function SettingsOrganizationFormPage() {
  const t = await getTranslations("settings.organization");

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">{t("title")}</h1>
      <OrganizationForm />
    </section>
  );
}
