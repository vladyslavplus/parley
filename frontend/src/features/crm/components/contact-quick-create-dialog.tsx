"use client";

import { useTranslations } from "next-intl";
import { toast } from "sonner";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { useApiForm } from "@/components/forms/use-api-form";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { ERROR_CODES } from "@/constants/error-codes";
import { LIMITS, SENTINELS } from "@/constants/limits";
import { QuickCreateDialogShell } from "@/features/crm/components/quick-create-dialog-shell";
import { useCompanies } from "@/features/crm/hooks/use-companies";
import { useCrmMutations } from "@/features/crm/hooks/use-crm-mutations";
import {
  createContactQuickSchema,
  type ContactQuickValues,
} from "@/features/crm/schemas/contact-schema";
import { useMe } from "@/lib/api/hooks/use-api";

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
  const companiesQuery = useCompanies({ limit: LIMITS.crmLookupLimit });
  const { createContact } = useCrmMutations();

  const companies = companiesQuery.data?.items ?? [];
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
          companyId:
            values.companyId && values.companyId !== SENTINELS.all
              ? values.companyId
              : null,
          ownerUserId: me?.user.id ?? null,
        });
        toast.success(t("success"));
        form.reset();
        onOpenChange(false);
        onCreated?.(created.id);
      },
    });

  const { errors, isSubmitting } = form.formState;

  return (
    <QuickCreateDialogShell
      open={open}
      onOpenChange={onOpenChange}
      title={t("title")}
      description={t("description")}
    >
      <ApiForm
        onSubmit={onSubmit}
        formError={formError}
        alertRef={alertRef}
        pending={isSubmitting}
        submitLabel={t("submit")}
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
          <Select
            value={form.watch("companyId") || SENTINELS.all}
            onValueChange={(val) =>
              form.setValue("companyId", val === SENTINELS.all ? "" : val)
            }
          >
            <SelectTrigger className="h-8 text-xs bg-background">
              <SelectValue placeholder={t("noCompany")} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value={SENTINELS.all}>{t("noCompany")}</SelectItem>
              {companies.map((c) => (
                <SelectItem key={c.id} value={c.id}>
                  {c.name}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </FormField>
      </ApiForm>
    </QuickCreateDialogShell>
  );
}
