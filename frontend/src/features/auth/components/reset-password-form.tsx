"use client";

import { useLocale, useTranslations } from "next-intl";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { Input } from "@/components/ui/input";
import { API_ROUTES, ROUTES } from "@/constants/routes";
import { useApiForm } from "@/components/forms/use-api-form";
import { useRouter } from "@/i18n/navigation";
import { bffFetch } from "@/lib/api/bff-client";
import {
  createResetPasswordSchema,
  type ResetPasswordValues,
} from "@/features/auth/schemas/auth-schemas";

export function ResetPasswordForm({
  userId,
  token,
}: {
  userId: string;
  token: string;
}) {
  const t = useTranslations("auth.reset");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const router = useRouter();
  const schema = createResetPasswordSchema(tValidation);

  const { form, onSubmit, formError, alertRef } =
    useApiForm<ResetPasswordValues>({
      schema,
      defaultValues: { newPassword: "", confirmPassword: "" },
      fields: ["newPassword", "confirmPassword"],
      submit: async (values) => {
        await bffFetch(API_ROUTES.resetPassword, {
          method: "POST",
          body: { userId, token, newPassword: values.newPassword },
          locale,
        });
        router.replace(ROUTES.login);
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
      <FormField label={t("newPassword")} error={errors.newPassword?.message}>
        <Input
          type="password"
          autoComplete="new-password"
          autoFocus
          {...form.register("newPassword")}
        />
      </FormField>
      <FormField
        label={t("confirmPassword")}
        error={errors.confirmPassword?.message}
      >
        <Input
          type="password"
          autoComplete="new-password"
          {...form.register("confirmPassword")}
        />
      </FormField>
    </ApiForm>
  );
}
