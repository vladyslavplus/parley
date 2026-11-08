"use client";

import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "next/navigation";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { Input } from "@/components/ui/input";
import { API_ROUTES, ROUTES } from "@/constants/routes";
import { useApiForm } from "@/components/forms/use-api-form";
import { bffFetch } from "@/lib/api/bff-client";
import {
  createAcceptInvitationSchema,
  type AcceptInvitationValues,
} from "@/features/auth/schemas/auth-schemas";

export function AcceptInvitationForm({ token }: { token: string }) {
  const t = useTranslations("auth.acceptInvitation");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const router = useRouter();
  const schema = createAcceptInvitationSchema(tValidation);

  const { form, onSubmit, formError, alertRef } =
    useApiForm<AcceptInvitationValues>({
      schema,
      defaultValues: { password: "", confirmPassword: "" },
      fields: ["password", "confirmPassword"],
      submit: async (values) => {
        await bffFetch(API_ROUTES.acceptInvitation, {
          method: "POST",
          body: { token, password: values.password },
          locale,
        });
        router.replace(`/${locale}${ROUTES.settingsProfile}`);
        router.refresh();
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
      <FormField label={t("password")} error={errors.password?.message}>
        <Input
          type="password"
          autoComplete="new-password"
          autoFocus
          {...form.register("password")}
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
