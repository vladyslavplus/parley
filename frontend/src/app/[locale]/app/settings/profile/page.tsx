import { getTranslations } from "next-intl/server";

import { ProfileForm } from "@/features/profile/components/profile-form";

export default async function SettingsProfileFormPage() {
  const t = await getTranslations("settings.profile");

  return (
    <section className="flex flex-col gap-4">
      <h1 className="text-xl font-semibold">{t("title")}</h1>
      <ProfileForm />
    </section>
  );
}
