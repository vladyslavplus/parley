"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useTheme } from "next-themes";

import { TableProgressBar } from "@/components/data-table/table-progress-bar";
import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { FormSelect } from "@/components/forms/form-select";
import { useApiForm } from "@/components/forms/use-api-form";
import { LoadingBlock } from "@/components/layout/loading-block";
import { Input } from "@/components/ui/input";
import { queryKeys } from "@/constants/query-keys";
import {
  createProfileSchema,
  type ProfileValues,
} from "@/features/profile/schemas/profile-schema";
import { usePathname, useRouter } from "@/i18n/navigation";
import { LOCALES } from "@/i18n/routing";
import { backendVoid } from "@/lib/api/bff-client";
import { useMe } from "@/lib/api/hooks/use-api";
import type { Schemas } from "@/lib/api/schemas";
import { timeZoneOptions } from "@/lib/time-zones";
import { showSuccessToast } from "@/lib/toasts";

const THEMES = ["system", "light", "dark"] as const;

function ProfileFormBody({
  profile,
  isFetching,
}: {
  profile: Schemas["UserProfileView"];
  isFetching: boolean;
}) {
  const t = useTranslations("settings.profile");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const pathname = usePathname();
  const router = useRouter();
  const queryClient = useQueryClient();
  const { setTheme } = useTheme();
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
      await backendVoid("/me/profile", { method: "PUT", body: values, locale });
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: queryKeys.me() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.members() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.owners() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.contactsAll() }),
        queryClient.invalidateQueries({ queryKey: queryKeys.companiesAll() }),
      ]);
      setTheme(values.theme);
      form.reset(values);
      showSuccessToast(t("saved"));
      if (values.locale !== locale)
        router.replace(pathname, {
          locale: values.locale as (typeof LOCALES)[number],
        });
    },
  });

  const { errors, isSubmitting, isDirty } = form.formState;
  return (
    <div className="relative w-full max-w-lg overflow-hidden rounded-[var(--radius-card)] border border-border bg-card p-6">
      <TableProgressBar active={isFetching} />
      <div className="flex flex-col gap-4">
        <ApiForm
          onSubmit={onSubmit}
          formError={formError}
          alertRef={alertRef}
          pending={isSubmitting}
          submitDisabled={!isDirty}
          submitLabel={t("submit")}
        >
          <FormField
            label={t("displayName")}
            error={errors.displayName?.message}
          >
            <Input autoComplete="name" {...form.register("displayName")} />
          </FormField>
          <FormField label={t("locale")} error={errors.locale?.message}>
            <FormSelect
              control={form.control}
              name="locale"
              options={LOCALES.map((code) => ({
                value: code,
                label: t(`locales.${code}`),
              }))}
            />
          </FormField>
          <FormField label={t("timeZone")} error={errors.timeZone?.message}>
            <FormSelect
              control={form.control}
              name="timeZone"
              options={zones.map((zone) => ({ value: zone, label: zone }))}
            />
          </FormField>
          <FormField label={t("theme")} error={errors.theme?.message}>
            <FormSelect
              control={form.control}
              name="theme"
              options={THEMES.map((theme) => ({
                value: theme,
                label: t(`themes.${theme}`),
              }))}
            />
          </FormField>
        </ApiForm>
      </div>
    </div>
  );
}

export function ProfileForm() {
  const { data: me, isPending, isFetching } = useMe();
  if (isPending && !me) {
    return <LoadingBlock />;
  }
  if (!me) {
    return null;
  }
  return (
    <ProfileFormBody
      profile={me.user}
      isFetching={Boolean(isFetching && !isPending)}
    />
  );
}
