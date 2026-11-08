"use client";

import { useTranslations } from "next-intl";
import { toast } from "sonner";

import { ApiForm } from "@/components/forms/api-form";
import { FormField } from "@/components/forms/form-field";
import { useApiForm } from "@/components/forms/use-api-form";
import { Input } from "@/components/ui/input";
import { QuickCreateDialogShell } from "@/features/crm/components/quick-create-dialog-shell";
import { useCrmMutations } from "@/features/crm/hooks/use-crm-mutations";
import {
  createCompanyQuickSchema,
  type CompanyQuickValues,
} from "@/features/crm/schemas/company-schema";
import { useMe } from "@/lib/api/hooks/use-api";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated?: (companyId: string) => void;
}

export function CompanyQuickCreateDialog({
  open,
  onOpenChange,
  onCreated,
}: Props) {
  const t = useTranslations("crm.companies.quickCreate");
  const tValidation = useTranslations("validation");
  const { data: me } = useMe();
  const { createCompany } = useCrmMutations();

  const schema = createCompanyQuickSchema((k, v) => tValidation(k, v));

  const { form, onSubmit, formError, alertRef } =
    useApiForm<CompanyQuickValues>({
      schema,
      defaultValues: {
        name: "",
        domain: "",
        country: "",
        industry: "",
      },
      fields: ["name", "domain", "country", "industry"],
      submit: async (values) => {
        const created = await createCompany({
          name: values.name,
          domain: values.domain || null,
          country: values.country ? values.country.toUpperCase() : null,
          industry: values.industry || null,
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
        {/* Field 1: Name (required) */}
        <FormField label={t("name")} error={errors.name?.message} required>
          <Input
            autoFocus
            {...form.register("name")}
            className="bg-background"
          />
        </FormField>

        {/* Field 2: Domain */}
        <FormField label={t("domain")} error={errors.domain?.message}>
          <Input
            placeholder="example.com"
            {...form.register("domain")}
            className="bg-background"
          />
        </FormField>

        {/* Field 3: Country */}
        <FormField label={t("country")} error={errors.country?.message}>
          <Input
            placeholder="UA"
            maxLength={2}
            {...form.register("country")}
            className="bg-background"
          />
        </FormField>

        {/* Field 4: Industry */}
        <FormField label={t("industry")} error={errors.industry?.message}>
          <Input {...form.register("industry")} className="bg-background" />
        </FormField>
      </ApiForm>
    </QuickCreateDialogShell>
  );
}
