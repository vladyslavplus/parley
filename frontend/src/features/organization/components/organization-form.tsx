"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";

import { TableProgressBar } from "@/components/data-table/table-progress-bar";
import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { FormSelect } from "@/components/forms/form-select";
import { useApiForm } from "@/components/forms/use-api-form";
import { LoadingBlock } from "@/components/layout/loading-block";
import { Alert } from "@/components/ui/alert";
import { Input } from "@/components/ui/input";
import { LIMITS } from "@/constants/limits";
import { PERMISSIONS } from "@/constants/permissions";
import { queryKeys } from "@/constants/query-keys";
import {
  createOrganizationSchema,
  type OrganizationValues,
} from "@/features/organization/schemas/organization-schema";
import { useCan } from "@/features/session/components/can";
import { LOCALES } from "@/i18n/routing";
import { backendVoid } from "@/lib/api/bff-client";
import { useMe } from "@/lib/api/hooks/use-api";
import type { Schemas } from "@/lib/api/schemas";
import { timeZoneOptions } from "@/lib/time-zones";
import { showSuccessToast } from "@/lib/toasts";

function OrganizationFormBody({
  organization,
  isFetching,
}: {
  organization: Schemas["CurrentOrganizationView"];
  isFetching: boolean;
}) {
  const t = useTranslations("settings.organization");
  const tProfile = useTranslations("settings.profile");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const queryClient = useQueryClient();
  const canManage = useCan(PERMISSIONS.orgManage);
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
        await backendVoid(
          "/organizations/{id}",
          { method: "PUT", body: values, locale },
          `/organizations/${organization.id}`,
        );
        await Promise.all([
          queryClient.invalidateQueries({ queryKey: queryKeys.me() }),
          queryClient.invalidateQueries({
            queryKey: queryKeys.myOrganizations(),
          }),
        ]);
        form.reset(values);
        showSuccessToast(t("saved"));
      },
    });

  const { errors, isSubmitting, isDirty } = form.formState;
  return (
    <div className="relative w-full max-w-lg overflow-hidden rounded-[var(--radius-card)] border border-border bg-card p-6">
      <TableProgressBar active={isFetching} />
      <div className="flex flex-col gap-4">
        {canManage ? null : <Alert tone="info">{t("readOnly")}</Alert>}
        <fieldset disabled={!canManage} className="contents">
          <ApiForm
            onSubmit={onSubmit}
            formError={formError}
            alertRef={alertRef}
            pending={isSubmitting || !canManage}
            submitDisabled={!isDirty}
            submitLabel={t("submit")}
          >
            <FormField label={t("name")} error={errors.name?.message}>
              <Input {...form.register("name")} />
            </FormField>
            <FormField label={t("timeZone")} error={errors.timeZone?.message}>
              <FormSelect
                control={form.control}
                name="timeZone"
                options={zones.map((zone) => ({ value: zone, label: zone }))}
              />
            </FormField>
            <FormField
              label={t("defaultLocale")}
              error={errors.defaultLocale?.message}
            >
              <FormSelect
                control={form.control}
                name="defaultLocale"
                options={LOCALES.map((code) => ({
                  value: code,
                  label: tProfile(`locales.${code}`),
                }))}
              />
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
    </div>
  );
}

export function OrganizationForm() {
  const { data: me, isPending, isFetching } = useMe();
  if (isPending && !me) {
    return <LoadingBlock />;
  }
  if (!me) {
    return null;
  }
  return (
    <OrganizationFormBody
      organization={me.organization}
      isFetching={Boolean(isFetching && !isPending)}
    />
  );
}
