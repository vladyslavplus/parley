"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { LoadingBlock } from "@/components/layout/loading-block";
import { Alert } from "@/components/ui/alert";
import { Select, Input } from "@/components/ui/input";
import { queryKeys } from "@/constants/query-keys";
import { LOCALES } from "@/i18n/routing";
import { usePathname, useRouter } from "@/i18n/navigation";
import { useApiForm } from "@/components/forms/use-api-form";
import { useMe } from "@/lib/api/hooks/use-api";
import { backendVoid } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";
import { timeZoneOptions } from "@/lib/time-zones";
import {
  createProfileSchema,
  type ProfileValues,
} from "@/features/profile/schemas/profile-schema";

const THEMES = ["system", "light", "dark"] as const;

function ProfileFormBody({ profile }: { profile: Schemas["UserProfileView"] }) {
  const t = useTranslations("settings.profile");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const pathname = usePathname();
  const router = useRouter();
  const queryClient = useQueryClient();
  const [saved, setSaved] = useState(false);
  const schema = createProfileSchema(tValidation);
  const zones = timeZoneOptions(profile.timeZone);

  const { form, onSubmit, formError, alertRef } = useApiForm<ProfileValues>({
    schema,
    defaultValues: {
      displayName: profile.displayName ?? "",
      locale: profile.locale,
      timeZone: profile.timeZone,
      theme: profile.theme,
    },
    fields: ["displayName", "locale", "timeZone", "theme"],
    submit: async (values) => {
      setSaved(false);
      await backendVoid("/me/profile", { method: "PUT", body: values, locale });
      await queryClient.invalidateQueries({ queryKey: queryKeys.me() });
      setSaved(true);
      if (values.locale !== locale)
        router.replace(pathname, {
          locale: values.locale as (typeof LOCALES)[number],
        });
    },
  });

  const { errors, isSubmitting } = form.formState;
  return (
    <div className="flex max-w-md flex-col gap-4">
      {saved ? <Alert tone="info">{t("saved")}</Alert> : null}
      <ApiForm
        onSubmit={onSubmit}
        formError={formError}
        alertRef={alertRef}
        pending={isSubmitting}
        submitLabel={t("submit")}
      >
        <FormField label={t("displayName")} error={errors.displayName?.message}>
          <Input autoComplete="name" {...form.register("displayName")} />
        </FormField>
        <FormField label={t("locale")} error={errors.locale?.message}>
          <Select {...form.register("locale")}>
            {LOCALES.map((code) => (
              <option key={code} value={code}>
                {t(`locales.${code}`)}
              </option>
            ))}
          </Select>
        </FormField>
        <FormField label={t("timeZone")} error={errors.timeZone?.message}>
          <Select {...form.register("timeZone")}>
            {zones.map((zone) => (
              <option key={zone} value={zone}>
                {zone}
              </option>
            ))}
          </Select>
        </FormField>
        <FormField label={t("theme")} error={errors.theme?.message}>
          <Select {...form.register("theme")}>
            {THEMES.map((theme) => (
              <option key={theme} value={theme}>
                {t(`themes.${theme}`)}
              </option>
            ))}
          </Select>
        </FormField>
      </ApiForm>
    </div>
  );
}

export function ProfileForm() {
  const { data: me } = useMe();
  return me ? <ProfileFormBody profile={me.user} /> : <LoadingBlock />;
}
