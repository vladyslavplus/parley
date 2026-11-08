"use client";

import { useLocale, useTranslations } from "next-intl";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { Input } from "@/components/ui/input";
import { API_ROUTES, ROUTES, ROUTE_PARAMS } from "@/constants/routes";
import { useApiForm } from "@/components/forms/use-api-form";
import { useRouter } from "@/i18n/navigation";
import { bffFetch } from "@/lib/api/bff-client";
import {
  createRegisterSchema,
  type RegisterValues,
} from "@/features/auth/schemas/auth-schemas";

export function RegisterForm() {
  const t = useTranslations("auth.register");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const router = useRouter();
  const schema = createRegisterSchema(tValidation);

  const { form, onSubmit, formError, alertRef } = useApiForm<RegisterValues>({
    schema,
    defaultValues: { organizationName: "", email: "", password: "" },
    fields: ["organizationName", "email", "password"],
    submit: async (values) => {
      await bffFetch(API_ROUTES.register, {
        method: "POST",
        body: values,
        locale,
      });
      const query = new URLSearchParams({ [ROUTE_PARAMS.email]: values.email });
      router.push(`${ROUTES.checkEmail}?${query.toString()}`);
    },
  });

  const { errors, isSubmitting } = form.formState;
  return (
    <ApiForm
      onSubmit={onSubmit}
      formError={formError}
      alertRef={alertRef}
      pending={isSubmitting}
      submitLabel={t("submit")}
    >
      <FormField
        label={t("organizationName")}
        error={errors.organizationName?.message}
      >
        <Input
          autoComplete="organization"
          autoFocus
          {...form.register("organizationName")}
        />
      </FormField>
      <FormField label={t("email")} error={errors.email?.message}>
        <Input type="email" autoComplete="email" {...form.register("email")} />
      </FormField>
      <FormField label={t("password")} error={errors.password?.message}>
        <Input
          type="password"
          autoComplete="new-password"
          {...form.register("password")}
        />
      </FormField>
    </ApiForm>
  );
}
