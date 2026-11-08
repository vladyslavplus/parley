"use client";

import { useQueryClient } from "@tanstack/react-query";
import { useLocale, useTranslations } from "next-intl";
import { useRouter } from "next/navigation";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { Input } from "@/components/ui/input";
import { ERROR_CODES } from "@/constants/error-codes";
import { API_ROUTES, ROUTES } from "@/constants/routes";
import { useApiForm } from "@/components/forms/use-api-form";
import { bffFetch } from "@/lib/api/bff-client";
import { sanitizeReturnUrl } from "@/lib/session/return-url";
import {
  createLoginSchema,
  type LoginValues,
} from "@/features/auth/schemas/auth-schemas";

import { ResendConfirmation } from "./resend-confirmation";

export function LoginForm({ returnUrl }: { returnUrl: string | null }) {
  const t = useTranslations("auth.login");
  const tValidation = useTranslations("validation");
  const locale = useLocale();
  const router = useRouter();
  const queryClient = useQueryClient();
  const schema = createLoginSchema(tValidation);

  const { form, onSubmit, formError, errorCode, alertRef } =
    useApiForm<LoginValues>({
      schema,
      defaultValues: { email: "", password: "" },
      fields: ["email", "password"],
      submit: async (values) => {
        await bffFetch(API_ROUTES.login, {
          method: "POST",
          body: values,
          locale,
        });
        // Nothing cached before sign-in may leak into the new session.
        queryClient.clear();
        router.replace(
          sanitizeReturnUrl(returnUrl) ?? `/${locale}${ROUTES.settingsProfile}`,
        );
        router.refresh();
      },
    });

  const { errors, isSubmitting } = form.formState;
  return (
    <div className="flex flex-col gap-4">
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
            autoComplete="username"
            autoFocus
            {...form.register("email")}
          />
        </FormField>
        <FormField label={t("password")} error={errors.password?.message}>
          <Input
            type="password"
            autoComplete="current-password"
            {...form.register("password")}
          />
        </FormField>
      </ApiForm>
      {errorCode === ERROR_CODES.authEmailNotConfirmed ? (
        <ResendConfirmation email={form.getValues("email")} />
      ) : null}
    </div>
  );
}
