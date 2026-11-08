"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { useCan } from "@/features/session/components/can";
import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { LoadingBlock } from "@/components/layout/loading-block";
import { Alert } from "@/components/ui/alert";
import { Input, Select } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import { PERMISSIONS } from "@/constants/permissions";
import { queryKeys } from "@/constants/query-keys";
import { useMe } from "@/lib/api/hooks/use-api";
import { useMounted } from "@/lib/use-mounted";
import { useApiForm } from "@/components/forms/use-api-form";
import { LOCALES } from "@/i18n/routing";
import { backendVoid } from "@/lib/api/bff-client";
import type { Schemas } from "@/lib/api/schemas";
import { timeZoneOptions } from "@/lib/time-zones";
import {
  createOrganizationSchema,
  type OrganizationValues,
} from "@/features/organization/schemas/organization-schema";

function OrganizationFormBody({
  organization,
}: {
  organization: Schemas["CurrentOrganizationView"];
}) {
  const t = useTranslations("settings.organization");
  const tProfile = useTranslations("settings.profile");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const queryClient = useQueryClient();
  const canManage = useCan(PERMISSIONS.orgManage);
  const [saved, setSaved] = useState(false);
  const schema = createOrganizationSchema(tValidation);
  const zones = timeZoneOptions(organization.timeZone);

  const { form, onSubmit, formError, alertRef } =
    useApiForm<OrganizationValues>({
      schema,
      defaultValues: {
        name: organization.name,
        timeZone: organization.timeZone,
        defaultLocale: organization.defaultLocale,
        currency: organization.currency,
      },
      fields: ["name", "timeZone", "defaultLocale", "currency"],
      submit: async (values) => {
        setSaved(false);
        await backendVoid(
          "/organizations/{id}",
          { method: "PUT", body: values, locale },
          `/organizations/${organization.id}`,
        );
        await queryClient.invalidateQueries({ queryKey: queryKeys.me() });
        setSaved(true);
      },
    });

  const { errors, isSubmitting } = form.formState;
  return (
    <div className="flex max-w-md flex-col gap-4">
      {canManage ? null : <Alert tone="info">{t("readOnly")}</Alert>}
      {saved ? <Alert tone="info">{t("saved")}</Alert> : null}
      <fieldset disabled={!canManage} className="contents">
        <ApiForm
          onSubmit={onSubmit}
          formError={formError}
          alertRef={alertRef}
          pending={isSubmitting || !canManage}
          submitLabel={t("submit")}
        >
          <FormField label={t("name")} error={errors.name?.message}>
            <Input {...form.register("name")} />
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
          <FormField
            label={t("defaultLocale")}
            error={errors.defaultLocale?.message}
          >
            <Select {...form.register("defaultLocale")}>
              {LOCALES.map((code) => (
                <option key={code} value={code}>
                  {tProfile(`locales.${code}`)}
                </option>
              ))}
            </Select>
          </FormField>
          <FormField label={t("currency")} error={errors.currency?.message}>
            <Input
              maxLength={LIMITS.currencyCodeLength}
              autoCapitalize="characters"
              {...form.register("currency")}
            />
          </FormField>
        </ApiForm>
      </fieldset>
    </div>
  );
}

export function OrganizationForm() {
  const mounted = useMounted();
  const { data: me } = useMe();
  if (!mounted || !me) {
    return <LoadingBlock />;
  }
  return <OrganizationFormBody organization={me.organization} />;
}
