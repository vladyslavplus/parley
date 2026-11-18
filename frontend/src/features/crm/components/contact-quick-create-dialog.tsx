"use client";

import { useTranslations } from "next-intl";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { useApiForm } from "@/components/forms/use-api-form";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Modal } from "@/components/ui/modal";
import { ERROR_CODES } from "@/constants/error-codes";
import { CompanyPicker } from "@/features/crm/components/company-picker";
import { useCrmMutations } from "@/features/crm/hooks/use-crm-mutations";
import {
  createContactQuickSchema,
  type ContactQuickValues,
} from "@/features/crm/schemas/contact-schema";
import { useMe } from "@/lib/api/hooks/use-api";
import { showSuccessToast } from "@/lib/toasts";

const CONTACT_FORM_ID = "contact-quick-create-form";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated?: (contactId: string) => void;
}

export function ContactQuickCreateDialog({
  open,
  onOpenChange,
  onCreated,
}: Props) {
  const t = useTranslations("crm.contacts.quickCreate");
  const tValidation = useTranslations("validation");
  const { data: me } = useMe();
  const { createContact } = useCrmMutations();

  const schema = createContactQuickSchema((k, v) => tValidation(k, v));

  const { form, onSubmit, formError, alertRef } =
    useApiForm<ContactQuickValues>({
      schema,
      defaultValues: {
        firstName: "",
        lastName: "",
        email: "",
        phone: "",
        companyId: "",
      },
      fields: ["firstName", "lastName", "email", "phone", "companyId"],
      codeFields: {
        [ERROR_CODES.contactEmailTaken]: "email",
      },
      submit: async (values) => {
        const created = await createContact({
          firstName: values.firstName,
          lastName: values.lastName || null,
          email: values.email || null,
          phone: values.phone || null,
          companyId: values.companyId || null,
          ownerUserId: me?.user.id ?? null,
        });
        showSuccessToast(t("success"));
        form.reset();
        onOpenChange(false);
        onCreated?.(created.id);
      },
    });

  const { errors, isSubmitting } = form.formState;

  return (
    <Modal
      open={open}
      onOpenChange={onOpenChange}
      title={t("title")}
      description={t("description")}
      footer={
        <Button
          type="submit"
          form={CONTACT_FORM_ID}
          className="w-full"
          disabled={isSubmitting}
        >
          {t("submit")}
        </Button>
      }
    >
      <ApiForm
        id={CONTACT_FORM_ID}
        onSubmit={onSubmit}
        formError={formError}
        alertRef={alertRef}
        pending={isSubmitting}
      >
        {/* Field 1: First name (required) */}
        <FormField
          label={t("firstName")}
          error={errors.firstName?.message}
          required
        >
          <Input
            autoFocus
            {...form.register("firstName")}
            className="bg-background"
          />
        </FormField>

        {/* Field 2: Last name */}
        <FormField label={t("lastName")} error={errors.lastName?.message}>
          <Input {...form.register("lastName")} className="bg-background" />
        </FormField>

        {/* Field 3: Email */}
        <FormField label={t("email")} error={errors.email?.message}>
          <Input
            type="email"
            {...form.register("email")}
            className="bg-background"
          />
        </FormField>

        {/* Field 4: Phone */}
        <FormField label={t("phone")} error={errors.phone?.message}>
          <Input
            type="tel"
            {...form.register("phone")}
            className="bg-background"
          />
        </FormField>

        {/* Field 5: Company */}
        <FormField label={t("company")} error={errors.companyId?.message}>
          <CompanyPicker
            value={form.watch("companyId")}
            placeholder={t("noCompany")}
            onChange={(selectedId) => {
              form.setValue("companyId", selectedId ?? "", {
                shouldValidate: true,
                shouldDirty: true,
              });
            }}
          />
        </FormField>
      </ApiForm>
    </Modal>
  );
}
