"use client";

import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { Alert } from "@/components/ui/alert";
import { Input } from "@/components/ui/input";
import { API_ROUTES } from "@/constants/routes";
import { useApiForm } from "@/components/forms/use-api-form";
import { bffFetch } from "@/lib/api/bff-client";
import {
  createEmailSchema,
  type EmailValues,
} from "@/features/auth/schemas/auth-schemas";

export function ForgotPasswordForm() {
  const t = useTranslations("auth.forgot");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const [sent, setSent] = useState(false);
  const schema = createEmailSchema(tValidation);

  const { form, onSubmit, formError, alertRef } = useApiForm<EmailValues>({
    schema,
    defaultValues: { email: "" },
    fields: ["email"],
    submit: async (values) => {
      await bffFetch(API_ROUTES.forgotPassword, {
        method: "POST",
        body: values,
        locale,
      });
      setSent(true);
    },
  });

  if (sent) return <Alert tone="info">{t("sent")}</Alert>;
  const { errors, isSubmitting } = form.formState;
  return (
    <ApiForm
      onSubmit={onSubmit}
      formError={formError}
      alertRef={alertRef}
      pending={isSubmitting}
      submitLabel={t("submit")}
    >
      <FormField label={t("email")} error={errors.email?.message}>
        <Input
          type="email"
          autoComplete="email"
          autoFocus
          {...form.register("email")}
        />
      </FormField>
    </ApiForm>
  );
}
